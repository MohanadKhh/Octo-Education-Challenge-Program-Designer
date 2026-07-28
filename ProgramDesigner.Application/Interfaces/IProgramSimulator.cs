using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Domain.Entities;

namespace ProgramDesigner.Application.Interfaces;

/// <summary>
/// Interface for program tree simulation engine.
/// </summary>
public interface IProgramSimulator
{
    ProgramSimulationResult Simulate(Node root, HashSet<Guid> selectedNodeIds, HashSet<Guid> completedStepIds);
}
