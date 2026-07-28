namespace ProgramDesigner.Application.DTOs;

/// <summary>
/// Response DTO returned by POST /programs and GET /programs/:id.
/// </summary>
public class ProgramResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public NodeDto RootNode { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
