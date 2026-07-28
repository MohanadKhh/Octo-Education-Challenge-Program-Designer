namespace ProgramDesigner.Application.DTOs;

/// <summary>
/// Result of validating a program's prerequisite logic.
/// </summary>
public class ProgramValidationResult
{
    /// <summary>True if there are no impossible prerequisites. Warnings alone do not invalidate a program.</summary>
    public bool IsValid => ImpossiblePrerequisites.Count == 0;

    /// <summary>Prerequisites that are logically impossible (self-ref, containment cycle, forward-ref, graph cycle).</summary>
    public List<ValidationIssue> ImpossiblePrerequisites { get; set; } = new();

    /// <summary>Prerequisites whose targets are only conditionally reachable (under a Choice(N < M) ancestor).</summary>
    public List<ValidationIssue> ReachabilityWarnings { get; set; } = new();
}