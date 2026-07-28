using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Domain.Entities;
using ProgramDesigner.Domain.Enums;

namespace ProgramDesigner.Application.Services;

/// <summary>
/// Simulator service that evaluates a participant's progress through a program tree.
/// Accepts participant choices (which branch they selected in Choice groups) and completed steps.
/// Returns categorized states for all nodes: Completed, Unlocked, Blocked, or NotSelected.
/// Located in Application Services.
/// </summary>
public class ProgramSimulator : IProgramSimulator
{
    /// <summary>
    /// Simulate the program tree rooted at <paramref name="root"/>.
    /// </summary>
    /// <param name="root">The root group node of the program.</param>
    /// <param name="selectedNodeIds">Node IDs selected under Choice groups. If empty for a choice group, all branches are active.</param>
    /// <param name="completedStepIds">Step Node IDs that the participant has completed.</param>
    public ProgramSimulationResult Simulate(
        Node root,
        HashSet<Guid> selectedNodeIds,
        HashSet<Guid> completedStepIds)
    {
        var result = new ProgramSimulationResult();
        var nodeIndex = new Dictionary<Guid, Node>();
        var parentMap = new Dictionary<Guid, Node?>();
        BuildIndex(root, null, nodeIndex, parentMap);

        // 1. Determine active vs unselected nodes
        var activeNodes = new HashSet<Guid>();
        DetermineActiveNodes(root, selectedNodeIds, activeNodes);

        // 2. Determine completion status for all nodes
        var completedNodes = new HashSet<Guid>();
        EvaluateCompletions(root, activeNodes, completedStepIds, completedNodes);

        // 3. Categorize each node in the tree
        foreach (var node in nodeIndex.Values)
        {
            var status = new NodeStatus
            {
                NodeId = node.Id,
                NodeName = node.Name,
                NodeType = node.Type == NodeType.Step ? "step" : "group"
            };

            if (!activeNodes.Contains(node.Id))
            {
                // Skip nodes in unselected choice branches completely
                continue;
            }

            if (completedNodes.Contains(node.Id))
            {
                status.State = "Completed";
                result.Completed.Add(status);
                continue;
            }

            // Node is active and not yet completed — evaluate prerequisite
            if (node.PrerequisiteId.HasValue && nodeIndex.TryGetValue(node.PrerequisiteId.Value, out var prereqTarget))
            {
                if (completedNodes.Contains(prereqTarget.Id))
                {
                    status.State = "Unlocked";
                    result.Unlocked.Add(status);
                }
                else
                {
                    status.State = "Blocked";
                    status.Reason = $"Blocked: Prerequisite '{prereqTarget.Name}' is not completed yet.";
                    result.Blocked.Add(status);
                }
            }
            else
            {
                // No prerequisite -> Unlocked (ready to take)
                status.State = "Unlocked";
                result.Unlocked.Add(status);
            }
        }

        return result;
    }

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

    /// <summary>
    /// Recursively determine active nodes based on choice group selections.
    /// </summary>
    private static void DetermineActiveNodes(
        Node node,
        HashSet<Guid> selectedNodeIds,
        HashSet<Guid> activeNodes)
    {
        activeNodes.Add(node.Id);

        if (node.Type != NodeType.Group) return;

        if (node.Rule == GroupRule.Choice && node.Children.Count > 0)
        {
            // Check if any child of this choice group is explicitly selected in selectedNodeIds
            var explicitlySelectedChildren = node.Children
                .Where(c => selectedNodeIds.Contains(c.Id))
                .ToList();

            if (explicitlySelectedChildren.Count > 0)
            {
                // Only selected children branches are active
                foreach (var child in explicitlySelectedChildren)
                {
                    DetermineActiveNodes(child, selectedNodeIds, activeNodes);
                }
                return;
            }
        }

        // If not a Choice group OR no selections provided for this choice group, all children are active
        foreach (var child in node.Children)
        {
            DetermineActiveNodes(child, selectedNodeIds, activeNodes);
        }
    }

    /// <summary>
    /// Evaluate which active nodes are completed.
    /// Bottom-up evaluation so groups complete when their child conditions are met.
    /// </summary>
    private static bool EvaluateCompletions(
        Node node,
        HashSet<Guid> activeNodes,
        HashSet<Guid> completedStepIds,
        HashSet<Guid> completedNodes)
    {
        if (!activeNodes.Contains(node.Id)) return false;

        if (node.Type == NodeType.Step)
        {
            bool isComplete = completedStepIds.Contains(node.Id);
            if (isComplete) completedNodes.Add(node.Id);
            return isComplete;
        }

        // Group evaluation
        var activeChildren = node.Children.Where(c => activeNodes.Contains(c.Id)).ToList();
        int completedChildrenCount = 0;

        foreach (var child in activeChildren)
        {
            if (EvaluateCompletions(child, activeNodes, completedStepIds, completedNodes))
            {
                completedChildrenCount++;
            }
        }

        bool groupCompleted = false;

        if (node.Rule == GroupRule.InOrder)
        {
            groupCompleted = activeChildren.Count > 0 && completedChildrenCount == activeChildren.Count;
        }
        else if (node.Rule == GroupRule.Choice)
        {
            int requiredCount = node.ChoiceCount ?? 1;
            groupCompleted = completedChildrenCount >= requiredCount;
        }

        if (groupCompleted)
        {
            completedNodes.Add(node.Id);
        }

        return groupCompleted;
    }
}
