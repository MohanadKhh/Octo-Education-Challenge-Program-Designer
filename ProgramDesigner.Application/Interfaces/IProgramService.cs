using ProgramDesigner.Application.Common;
using ProgramDesigner.Application.DTOs;

namespace ProgramDesigner.Application.Interfaces;

public interface IProgramService
{
    Task<GeneralResult<ProgramResponse>> CreateProgramAsync(CreateProgramRequest request);
    Task<GeneralResult<ProgramResponse>> GetProgramAsync(Guid id);
    Task<GeneralResult<ProgramValidationResult>> ValidateProgramAsync(Guid id);
    Task<GeneralResult<ProgramSimulationResult>> SimulateProgramAsync(Guid id, SimulateProgramRequest request);
}