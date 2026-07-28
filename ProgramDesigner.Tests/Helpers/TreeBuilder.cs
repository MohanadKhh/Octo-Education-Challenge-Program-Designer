using ProgramDesigner.Domain.Entities;
using ProgramDesigner.Domain.Enums;

namespace ProgramDesigner.Tests.Helpers;

/// <summary>
/// Fluent builder for constructing test trees concisely.
/// Usage:
///   TreeBuilder.Group("Root", GroupRule.InOrder)
///       .WithChild(TreeBuilder.Step("A"))
///       .WithChild(TreeBuilder.Group("B", GroupRule.Choice, 2)
///           .WithChild(TreeBuilder.Step("B1"))
///           .WithChild(TreeBuilder.Step("B2"))
///           .WithChild(TreeBuilder.Step("B3")))
///       .Build();
/// </summary>
public static class TreeBuilder
{
    /// <summary>Create a Step node.</summary>
    public static NodeBuilder Step(string name, string? stepType = null)
    {
        return new NodeBuilder(new Node
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = NodeType.Step,
            StepType = stepType
        });
    }

    /// <summary>Create a Group node.</summary>
    public static NodeBuilder Group(string name, GroupRule rule, int? choiceCount = null)
    {
        return new NodeBuilder(new Node
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = NodeType.Group,
            Rule = rule,
            ChoiceCount = choiceCount
        });
    }

    public class NodeBuilder
    {
        private readonly Node _node;
        private int _childOrder;

        internal NodeBuilder(Node node) => _node = node;

        /// <summary>Get the node's Id (for setting prerequisites).</summary>
        public Guid Id => _node.Id;

        /// <summary>Add a child node.</summary>
        public NodeBuilder WithChild(NodeBuilder child)
        {
            var childNode = child.Build();
            childNode.Order = _childOrder++;
            childNode.ParentNodeId = _node.Id;
            _node.Children.Add(childNode);
            return this;
        }

        /// <summary>Set a prerequisite on this node.</summary>
        public NodeBuilder WithPrerequisite(Guid prerequisiteId)
        {
            _node.PrerequisiteId = prerequisiteId;
            return this;
        }

        /// <summary>Set a prerequisite pointing to another builder's node.</summary>
        public NodeBuilder WithPrerequisite(NodeBuilder target)
        {
            _node.PrerequisiteId = target.Id;
            return this;
        }

        /// <summary>Set a specific Id (useful for self-reference tests).</summary>
        public NodeBuilder WithId(Guid id)
        {
            _node.Id = id;
            return this;
        }

        /// <summary>Build and return the Node.</summary>
        public Node Build() => _node;
    }
}
