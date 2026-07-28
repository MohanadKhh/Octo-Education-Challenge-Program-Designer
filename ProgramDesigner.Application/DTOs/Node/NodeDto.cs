namespace ProgramDesigner.Application.DTOs;

/// <summary>
/// Recursive DTO representing a node in the program tree (used for both request and response).
/// </summary>
public class NodeDto
{
    /// <summary>Null on create requests; assigned by the server on responses.</summary>
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>"step" or "group".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Step-specific: the kind of step (e.g. "attend session"). Null for groups.</summary>
    public string? StepType { get; set; }

    /// <summary>"inOrder" or "choice". Null for steps.</summary>
    public string? Rule { get; set; }

    /// <summary>For Choice groups: pick N of M. Null otherwise.</summary>
    public int? ChoiceCount { get; set; }

    /// <summary>
    /// References another node by Id.
    /// Included in responses alongside PrerequisiteName when a prerequisite is present.
    /// </summary>
    public Guid? PrerequisiteId { get; set; }

    /// <summary>
    /// References another node by its unique name.
    /// Included in responses alongside PrerequisiteId when a prerequisite is present.
    /// </summary>
    public string? PrerequisiteName { get; set; }

    /// <summary>Children nodes. Null/omitted when a node has no children.</summary>
    public List<NodeDto>? Children { get; set; }
}
