namespace ProgramDesigner.Application.DTOs;

public class NodeStatus
{
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty; // "step" or "group"
    public string State { get; set; } = string.Empty;    // "Completed", "Unlocked", "Blocked"
    public string? Reason { get; set; }
}
