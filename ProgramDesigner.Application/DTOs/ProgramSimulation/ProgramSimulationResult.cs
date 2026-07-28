namespace ProgramDesigner.Application.DTOs;

/// <summary>
/// Model for the simulation result.
/// Categorizes active program nodes into Completed, Unlocked (ready to start), or Blocked (waiting on prerequisites).
/// </summary>
public class ProgramSimulationResult
{
    public List<NodeStatus> Completed { get; set; } = new();
    public List<NodeStatus> Unlocked { get; set; } = new();
    public List<NodeStatus> Blocked { get; set; } = new();
}
