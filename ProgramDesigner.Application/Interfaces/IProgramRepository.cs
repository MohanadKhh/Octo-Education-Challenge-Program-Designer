using ProgramDesigner.Domain.Entities;

namespace ProgramDesigner.Application.Interfaces;

/// <summary>
/// Repository abstraction for learning programs.
/// Defined in the Application layer; implemented in Infrastructure.
/// </summary>
public interface IProgramRepository
{
    Task AddAsync(LearningProgram program);
    Task<LearningProgram?> GetByIdAsync(Guid id);
}
