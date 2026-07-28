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
/// 1. Impossible prerequisites → reject (self-ref, containment cycle, forward-ref, graph cycle)
/// 2. Reachability warnings → warn (target under a Choice(N &lt; M) ancestor)
/// </summary>
public class ProgramValidator : IProgramValidator
{
    /// <summary>
    /// Validate the prerequisite logic of a program tree rooted at <paramref name="root"/>.
    /// </summary>
    public ProgramValidationResult Validate(Node root)
    {
        var result = new ProgramValidationResult();

        // ── Phase 0: Build indexes ─────────────────────────────────────
        var nodeIndex = new Dictionary<Guid, Node>();   // id → node
        var parentMap = new Dictionary<Guid, Node?>();   // id → parent node (null for root)
        BuildIndex(root, null, nodeIndex, parentMap);

        // ── Phase 1: Per-prerequisite impossible checks ────────────────
        var validPrerequisiteEdges = new List<(Node source, Node target)>();

        foreach (var node in nodeIndex.Values)
        {
            if (node.PrerequisiteId is null) continue;

            if (!nodeIndex.TryGetValue(node.PrerequisiteId.Value, out var target))
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, node.PrerequisiteId, null,
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

            // 1b. Containment cycle — node depends on a descendant OR an ancestor container
            if (node.Type == NodeType.Group && IsDescendant(node, target.Id))
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, target,
                    $"'{node.Name}' depends on '{target.Name}', which is contained inside it (containment cycle)."));
                continue;
            }

            if (target.Type == NodeType.Group && IsDescendant(target, node.Id))
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, target,
                    $"'{node.Name}' depends on '{target.Name}', which is a container that contains it (containment cycle)."));
                continue;
            }

            // 1c. Forward-reference in InOrder sequence
            if (IsForwardReferenceInOrder(node, target, parentMap, nodeIndex))
            {
                result.ImpossiblePrerequisites.Add(MakeIssue(node, target,
                    $"'{node.Name}' depends on '{target.Name}', which comes later in an InOrder sequence (forward reference)."));
                continue;
            }

            // Not caught by simple checks — save for graph-level cycle detection
            validPrerequisiteEdges.Add((node, target));
        }

        // 1d. General graph cycle detection (catches mutual/indirect cycles like Tree 3)
        var cycleIssues = DetectCycles(validPrerequisiteEdges, nodeIndex);
        result.ImpossiblePrerequisites.AddRange(cycleIssues);

        // ── Phase 2: Reachability warnings ─────────────────────────────
        // Only check edges not already flagged as impossible
        var impossibleNodeIds = new HashSet<Guid>(
            result.ImpossiblePrerequisites.Select(i => i.NodeId));

        foreach (var node in nodeIndex.Values)
        {
            if (node.PrerequisiteId is null) continue;
            if (impossibleNodeIds.Contains(node.Id)) continue;
            if (!nodeIndex.TryGetValue(node.PrerequisiteId.Value, out var target)) continue;

            var warning = CheckReachability(node, target, parentMap, nodeIndex);
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
    // Impossible prerequisite checks
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Check if <paramref name="candidateId"/> is a descendant of <paramref name="ancestor"/>.</summary>
    private static bool IsDescendant(Node ancestor, Guid candidateId)
    {
        foreach (var child in ancestor.Children)
        {
            if (child.Id == candidateId) return true;
            if (IsDescendant(child, candidateId)) return true;
        }
        return false;
    }

    /// <summary>
    /// Check if target comes strictly later than source within a shared InOrder ancestor.
    /// Finds the lowest common ancestor (LCA) and, if it's InOrder, compares branch positions.
    /// </summary>
    private static bool IsForwardReferenceInOrder(
        Node source, Node target,
        Dictionary<Guid, Node?> parentMap,
        Dictionary<Guid, Node> nodeIndex)
    {
        var sourceChain = GetAncestorChain(source.Id, parentMap, nodeIndex); // [self, parent, ..., root]
        var targetChain = GetAncestorChain(target.Id, parentMap, nodeIndex);

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
    /// </summary>
    private static List<ValidationIssue> DetectCycles(
        List<(Node source, Node target)> edges,
        Dictionary<Guid, Node> nodeIndex)
    {
        var issues = new List<ValidationIssue>();

        // Build adjacency list: source.Id → [target.Id, ...]
        var adjacency = new Dictionary<Guid, List<Guid>>();
        var edgeSourceNodes = new HashSet<Guid>();

        foreach (var (source, target) in edges)
        {
            if (!adjacency.ContainsKey(source.Id))
                adjacency[source.Id] = new();
            adjacency[source.Id].Add(target.Id);
            edgeSourceNodes.Add(source.Id);
        }

        if (adjacency.Count == 0) return issues;

        // DFS coloring: White → Gray → Black
        var white = new HashSet<Guid>(nodeIndex.Keys);
        var gray = new HashSet<Guid>();
        var black = new HashSet<Guid>();
        var cycleParticipants = new HashSet<Guid>();
        var path = new List<Guid>();

        foreach (var nodeId in nodeIndex.Keys)
        {
            if (!white.Contains(nodeId)) continue;
            DfsCycleDetect(nodeId, adjacency, white, gray, black, cycleParticipants, path);
        }

        // Report cycle issues for nodes that have prerequisites and are in the cycle
        foreach (var nodeId in cycleParticipants)
        {
            if (!edgeSourceNodes.Contains(nodeId)) continue;
            var node = nodeIndex[nodeId];
            if (node.PrerequisiteId is null) continue;
            if (!nodeIndex.TryGetValue(node.PrerequisiteId.Value, out var target)) continue;

            issues.Add(MakeIssue(node, target,
                $"'{node.Name}' is part of a prerequisite cycle involving '{target.Name}' (mutual/indirect dependency)."));
        }

        return issues;
    }

    private static void DfsCycleDetect(
        Guid nodeId,
        Dictionary<Guid, List<Guid>> adjacency,
        HashSet<Guid> white,
        HashSet<Guid> gray,
        HashSet<Guid> black,
        HashSet<Guid> cycleParticipants,
        List<Guid> path)
    {
        white.Remove(nodeId);
        gray.Add(nodeId);
        path.Add(nodeId);

        if (adjacency.TryGetValue(nodeId, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (gray.Contains(neighbor))
                {
                    // Cycle found — record all nodes from the cycle start to current
                    int cycleStart = path.IndexOf(neighbor);
                    for (int i = cycleStart; i < path.Count; i++)
                        cycleParticipants.Add(path[i]);
                }
                else if (white.Contains(neighbor))
                {
                    DfsCycleDetect(neighbor, adjacency, white, gray, black, cycleParticipants, path);
                }
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
    /// Check if a prerequisite target is only conditionally reachable.
    /// 
    /// For each Choice(N &lt; M) ancestor along the target's path to the root,
    /// we check whether the source node is also under the SAME branch of that
    /// Choice group. If source shares the branch, the Choice is irrelevant
    /// (both are co-selected). If source is NOT under the same branch (or is
    /// outside the Choice group entirely), the target is conditional.
    /// 
    /// Key distinction: a prerequisite on a Choice group AS A WHOLE is safe —
    /// the group will be reached. Only a specific child INSIDE a Choice(N &lt; M)
    /// group is conditional.
    /// </summary>
    private static string? CheckReachability(
        Node source, Node target,
        Dictionary<Guid, Node?> parentMap,
        Dictionary<Guid, Node> nodeIndex)
    {
        // Build the source's ancestor set for fast lookups
        var sourceAncestorIds = new HashSet<Guid>();
        var cur = source;
        while (true)
        {
            sourceAncestorIds.Add(cur.Id);
            if (!parentMap.TryGetValue(cur.Id, out var p) || p is null) break;
            cur = p;
        }

        var conditionalReasons = new List<string>();
        var current = target;

        while (true)
        {
            if (!parentMap.TryGetValue(current.Id, out var parent) || parent is null)
                break; // Reached root

            // Is parent a Choice(N < M) group?
            if (parent.Type == NodeType.Group &&
                parent.Rule == GroupRule.Choice &&
                parent.ChoiceCount.HasValue &&
                parent.ChoiceCount.Value < parent.Children.Count)
            {
                // `current` is the direct child branch of this Choice group on the target's path.
                // Check if source is also inside this same branch (i.e. a descendant of `current`).
                bool sourceInSameBranch = sourceAncestorIds.Contains(current.Id);

                if (!sourceInSameBranch)
                {
                    // Source is NOT under the same branch — this Choice makes the target conditional
                    conditionalReasons.Add(
                        $"'{current.Name}' is inside Choice group '{parent.Name}' " +
                        $"(pick {parent.ChoiceCount} of {parent.Children.Count}), so it may not be selected");
                }
            }

            current = parent;
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
