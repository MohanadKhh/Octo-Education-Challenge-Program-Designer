namespace ProgramDesigner.Application.DTOs;

/// <summary>Request body for POST /programs.</summary>
public class CreateProgramRequest
{
    public string Name { get; set; } = string.Empty;
    public NodeDto RootNode { get; set; } = null!;
}
