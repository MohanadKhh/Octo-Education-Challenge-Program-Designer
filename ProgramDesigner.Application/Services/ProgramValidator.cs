using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Domain.Entities;
using ProgramDesigner.Domain.Enums;

namespace ProgramDesigner.Application.Services;

/// <summary>
/// Validation engine for program trees.
/// Located in Application Services.
/// 
/// Validates two categories:
/// 1. Impossible prerequisites → reject (self-ref, direct mutual, containment cycle (inside and outside), forward-ref, graph cycle)
/// 2. Reachability warnings → warn (target under a Choice(N < M) ancestor)
/// </summary>
public class ProgramValidator : IProgramValidator
{
    /// <summary>
    /// Validate the prerequisite logic of a program tree rooted at <paramref name="root"/>.
    /// </summary>
    public ProgramValidationResult Validate(Node root)
    {
        var result = new ProgramValidationResult();

        // ── Phase 0: Build tree indexes ─────────────────────────────────
        var nodeIndex = new Dictionary<Guid, Node>();   // id → node
        var parentMap = new Dictionary<Guid, Node?>();   // id → parent node (null for root)
        BuildIndex(root, null, nodeIndex, parentMap);

        // ── Phase 1: Per-prerequisite impossible checks ────────────────
        // Map: sourceNodeId → (source, target, sourceAncestors, targetAncestors)
        var validPrerequisiteEdges = new Dictionary<Guid, (Node source, Node target, List<Node> sourceAncestors, List<Node> targetAncestors)>();

        foreach (var node in nodeIndex.Values)
        {
            if (node.PrerequisiteId is null) continue;

            if (!nodeIndex.TryGetValue(node.PrerequisiteId.Value, out var target))
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, node.PrerequisiteId.Value, null,
                    $"Prerequisite target '{node.PrerequisiteId}' does not exist in the program tree."));
                continue;
            }

            // 1a. Self-reference
            if (node.Id == target.Id)
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, target,
                    $"'{node.Name}' has a prerequisite on itself (self-reference)."));
                continue;
            }

            // 1b. Direct Mutual Cycle (A -> B and B -> A)
            if (target.PrerequisiteId.HasValue && target.PrerequisiteId.Value == node.Id)
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, target,
                    $"'{node.Name}' and '{target.Name}' form a direct mutual prerequisite cycle."));
                continue;
            }

            // Precompute ancestor chains once for both nodes: [self, parent, grandparent, ..., root]
            var sourceAncestors = GetAncestorChain(node.Id, parentMap, nodeIndex);
            var targetAncestors = GetAncestorChain(target.Id, parentMap, nodeIndex);

            // 1c. Containment cycle — parent group depends on its own child/descendant
            if (node.Type == NodeType.Group && targetAncestors.Contains(node))
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, target,
                    $"'{node.Name}' depends on '{target.Name}', which is parent depending on one of his descendants (containment cycle)."));
                continue;
            }

            // 1d. Containment cycle — child depends on an ancestor container
            if (target.Type == NodeType.Group && sourceAncestors.Contains(target))
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, target,
                    $"'{node.Name}' depends on '{target.Name}', which is child depending on one of his ancestors (containment cycle)."));
                continue;
            }

            // 1e. Forward-reference in InOrder sequence (using precomputed ancestor chains)
            if (IsForwardReferenceInOrder(sourceAncestors, targetAncestors))
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, target,
                    $"'{node.Name}' depends on '{target.Name}', which comes later in an InOrder sequence (forward reference)."));
                continue;
            }

            // Not caught by simple checks — save for graph-level cycle detection & reachability warnings
            validPrerequisiteEdges[node.Id] = (node, target, sourceAncestors, targetAncestors);
        }

        // 1f. Indirect Mutual Cycle (A -> B and B -> C and C -> A)
        var cycleIssues = DetectCycles(validPrerequisiteEdges);
        result.ImpossiblePrerequisites.AddRange(cycleIssues);

        // ── Phase 2: Reachability warnings ─────────────────────────────
        // validPrerequisiteEdges now contains only clean non-impossible edges
        foreach (var (node, target, sourceAncestors, targetAncestors) in validPrerequisiteEdges.Values)
        {
            var warning = CheckReachability(node, target, sourceAncestors, targetAncestors);
            if (warning is not null)
            {
                result.ReachabilityWarnings.Add(MakeIssue(node, target, warning));
            }
        }

        return result;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Index building
    // ═══════════════════════════════════════════════════════════════════
    private static void BuildIndex(
        Node node, Node? parent,
        Dictionary<Guid, Node> index,
        Dictionary<Guid, Node?> parentMap)
    {
        index[node.Id] = node;
        parentMap[node.Id] = parent;

        foreach (var child in node.Children)
            BuildIndex(child, node, index, parentMap);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Forward-reference detection
    // ═══════════════════════════════════════════════════════════════════
    /// <summary>
    /// Check if target comes strictly later than source within a shared InOrder ancestor.
    /// Finds the lowest common ancestor (LCA) and, if it's InOrder, compares branch positions.
    /// </summary>
    private static bool IsForwardReferenceInOrder(
        List<Node> sourceChain,
        List<Node> targetChain)
    {
        // Find LCA: first node that appears in both chains
        var sourceIds = new HashSet<Guid>(sourceChain.Select(n => n.Id));

        Node? lca = null;
        int targetLcaIndex = -1;
        for (int i = 0; i < targetChain.Count; i++)
        {
            if (sourceIds.Contains(targetChain[i].Id))
            {
                lca = targetChain[i];
                targetLcaIndex = i;
                break;
            }
        }

        // Check lca is null as safeguard (rootNode will be lca at least)
        if (lca is null || lca.Rule != GroupRule.InOrder) return false;

        // Find the direct child of LCA on the source branch
        int sourceLcaIndex = sourceChain.FindIndex(n => n.Id == lca.Id);

        // That be Impossible Prerequisites but handled with IsDescendant but that not Forward Reference Rejected
        if (sourceLcaIndex <= 0 || targetLcaIndex <= 0) return false;

        // this is done to find the child of LCA that has the source branch and the target branch to know who come first from shared parent
        var sourceDirectChild = sourceChain[sourceLcaIndex - 1]; // one step below LCA on source path
        var targetDirectChild = targetChain[targetLcaIndex - 1]; // one step below LCA on target path

        int sourcePos = lca.Children.FindIndex(c => c.Id == sourceDirectChild.Id);
        int targetPos = lca.Children.FindIndex(c => c.Id == targetDirectChild.Id);

        // Forward reference: target is positioned AFTER source in InOrder
        return targetPos > sourcePos;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Build ancestor chains
    // ═══════════════════════════════════════════════════════════════════
    /// <summary>
    /// Build ancestor chain from node to root: [self, parent, grandparent, ..., root].
    /// </summary>
    private static List<Node> GetAncestorChain(
        Guid nodeId,
        Dictionary<Guid, Node?> parentMap,
        Dictionary<Guid, Node> nodeIndex)
    {
        var chain = new List<Node>();
        Guid? current = nodeId;

        while (current.HasValue && nodeIndex.TryGetValue(current.Value, out var node))
        {
            chain.Add(node);
            if (!parentMap.TryGetValue(current.Value, out var parent) || parent is null)
                break;
            current = parent.Id;
        }

        return chain;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Graph cycle detection (DFS 3-coloring)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detect cycles in the prerequisite graph using DFS with 3-coloring.
    /// Only operates on edges that weren't already caught by simpler checks.
    /// Uses <paramref name="edges"/> directly as the adjacency map and removes cycle edges in O(1) time.
    /// </summary>
    private static List<ValidationIssue> DetectCycles(
        Dictionary<Guid, (Node source, Node target, List<Node> sourceAncestors, List<Node> targetAncestors)> edges)
    {
        var issues = new List<ValidationIssue>();
        if (edges.Count == 0) return issues;

        // DFS coloring: White → Gray → Black
        // Track unvisited nodes directly from edges.Keys (nodes with prerequisites)
        var white = new HashSet<Guid>(edges.Keys);
        var gray = new HashSet<Guid>();
        var black = new HashSet<Guid>();
        var cycleParticipants = new HashSet<Guid>();
        var path = new List<Guid>();

        // Loop directly over 'edges.Keys' instead of all nodes in tree
        foreach (var nodeId in edges.Keys)
        {
            if (!white.Contains(nodeId)) continue;
            DfsCycleDetect(nodeId, edges, white, gray, black, cycleParticipants, path);
        }

        // Report cycle issues for nodes in cycle & remove cycle edges in O(1) time
        foreach (var nodeId in cycleParticipants)
        {
            if (!edges.TryGetValue(nodeId, out var edge)) continue;

            issues.Add(MakeIssue(edge.source, edge.target,
                $"'{edge.source.Name}' depends on '{edge.target.Name}' is part of a prerequisites create indirect mutual cycle."));

            // O(1) instant removal from dictionary
            edges.Remove(nodeId);
        }

        return issues;
    }

    private static void DfsCycleDetect(
        Guid nodeId,
        Dictionary<Guid, (Node source, Node target, List<Node> sourceAncestors, List<Node> targetAncestors)> edges,
        HashSet<Guid> white,
        HashSet<Guid> gray,
        HashSet<Guid> black,
        HashSet<Guid> cycleParticipants,
        List<Guid> path)
    {
        white.Remove(nodeId);
        gray.Add(nodeId);
        path.Add(nodeId);

        // O(1) direct lookup from edges dictionary
        if (edges.TryGetValue(nodeId, out var edge))
        {
            var targetId = edge.target.Id;

            if (gray.Contains(targetId))
            {
                // Cycle found — record all nodes from cycle start to current
                int cycleStart = path.IndexOf(targetId);
                if (cycleStart >= 0)
                {
                    for (int i = cycleStart; i < path.Count; i++)
                        cycleParticipants.Add(path[i]);
                }
            }
            else if (white.Contains(targetId) || (!black.Contains(targetId) && edges.ContainsKey(targetId)))
            {
                DfsCycleDetect(targetId, edges, white, gray, black, cycleParticipants, path);
            }
        }

        path.RemoveAt(path.Count - 1);
        gray.Remove(nodeId);
        black.Add(nodeId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Reachability warnings
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Check if target is inside a Choice(N < M) container where target is not guaranteed to be chosen.
    /// Uses precomputed ancestor chains: [self, parent, grandparent, ..., root].
    /// </summary>
    private static string? CheckReachability(
        Node source, Node target,
        List<Node> sourceAncestors,
        List<Node> targetAncestors)
    {
        var sourceAncestorIds = new HashSet<Guid>(sourceAncestors.Select(n => n.Id));
        var conditionalReasons = new List<string>();

        // targetAncestors is [target, parent, grandparent, ..., root]
        for (int i = 0; i < targetAncestors.Count - 1; i++)
        {
            var current = targetAncestors[i];
            var parent = targetAncestors[i + 1];

            // Is parent a Choice(N < M) group?
            if (parent.Type == NodeType.Group &&
                parent.Rule == GroupRule.Choice &&
                parent.ChoiceCount.HasValue &&
                parent.ChoiceCount.Value < parent.Children.Count)
            {
                // `current` is the direct child branch of this Choice group on the target's path.
                // Check if source is also inside this same branch (i.e. a descendant of `current`).
                bool sourceInSameBranch = sourceAncestorIds.Contains(current.Id);

                if (sourceInSameBranch)
                {
                    // Co-selected under the same branch!
                    // All higher ancestors are also shared, so we can break early.
                    break;
                }

                // Source is NOT under the same branch — this Choice makes the target conditional
                conditionalReasons.Add(
                    $"'{current.Name}' is inside Choice group '{parent.Name}' " +
                    $"(pick {parent.ChoiceCount.Value} of {parent.Children.Count}), so it may not be selected");
            }
        }

        if (conditionalReasons.Count == 0)
            return null;

        return $"Prerequisite target '{target.Name}' is only conditionally reachable: " +
               string.Join("; additionally, ", conditionalReasons) + ".";
    }

    // ═══════════════════════════════════════════════════════════════════
    // Issue factory
    // ═══════════════════════════════════════════════════════════════════

    private static ValidationIssue MakeIssue(Node node, Node target, string reason) =>
        new()
        {
            NodeId = node.Id,
            NodeName = node.Name,
            PrerequisiteTargetId = target.Id,
            PrerequisiteTargetName = target.Name,
            Reason = reason
        };

    private static ValidationIssue MakeIssue(Node node, Guid? targetId, string? targetName, string reason) =>
        new()
        {
            NodeId = node.Id,
            NodeName = node.Name,
            PrerequisiteTargetId = targetId,
            PrerequisiteTargetName = targetName,
            Reason = reason
        };
}
