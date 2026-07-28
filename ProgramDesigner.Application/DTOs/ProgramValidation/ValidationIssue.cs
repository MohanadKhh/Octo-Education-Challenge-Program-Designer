namespace ProgramDesigner.Application.DTOs;

/// <summary>
/// A single validation finding — either an impossible prerequisite or a reachability warning.
/// </summary>
public class ValidationIssue
{
    /// <summary>The node that carries the prerequisite.</summary>
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;

    /// <summary>The target of the prerequisite.</summary>
    public Guid? PrerequisiteTargetId { get; set; }
    public string? PrerequisiteTargetName { get; set; }

    /// <summary>Human-readable explanation of why this prerequisite is problematic.</summary>
    public string Reason { get; set; } = string.Empty;
}
