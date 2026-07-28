using ProgramDesigner.Domain.Enums;

namespace ProgramDesigner.Domain.Entities;

/// <summary>
/// A single node in the program tree. Either a Step (leaf) or a Group (container).
/// Discriminated by <see cref="Type"/>, not by subclassing — the whole tree uses one recursive type.
/// </summary>
public class Node
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Human-readable name (e.g. "Foundations", "Machine Learning Basics").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this node is a Step (leaf) or a Group (container).</summary>
    public NodeType Type { get; set; }

    /// <summary>Optional prerequisite pointing at the ID of another node anywhere in the tree. Blocks this node (and everything inside it for Groups) until satisfied.</summary>
    public Guid? PrerequisiteId { get; set; }

    /// <summary>Zero-based position among siblings. Used to preserve child ordering.</summary>
    public int Order { get; set; }

    // ── Step-specific ──────────────────────────────────────────────────

    /// <summary>The kind of step (e.g. "attend session", "pass test"). Only meaningful when <see cref="Type"/> is <see cref="NodeType.Step"/>.</summary>
    public string? StepType { get; set; }

    // ── Group-specific ─────────────────────────────────────────────────

    /// <summary>Completion rule for a Group. Null for Steps.</summary>
    public GroupRule? Rule { get; set; }

    /// <summary>For Choice groups: the number of children the participant must pick (N in "pick N of M"). Null for InOrder groups and Steps.</summary>
    public int? ChoiceCount { get; set; }


    // ── EF Core navigation (not used by domain logic) ──────────────────

    /// <summary>Parent node ID for EF Core's self-referencing FK. Null for root nodes.</summary>
    public Guid? ParentNodeId { get; set; }

    /// <summary>The program this node belongs to. Set by EF Core.</summary>
    public Guid ProgramId { get; set; }

    // ── Navigational property ──
    /// <summary>Ordered children. Empty for Steps.</summary>
    public List<Node> Children { get; set; } = new();
}
