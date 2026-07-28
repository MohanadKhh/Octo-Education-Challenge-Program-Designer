using Microsoft.AspNetCore.Mvc;
using ProgramDesigner.Application.Common;
using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Application.Interfaces;

namespace ProgramDesigner.APIs.Controllers;

[ApiController]
[Route("[controller]")]
public class ProgramsController : ControllerBase
{
    private readonly IProgramService _programService;

    public ProgramsController(IProgramService programService)
    {
        _programService = programService;
    }

    /// <summary>
    /// Create a program from a JSON tree (root Group with nested Steps/Groups/rules/prerequisites).
    /// Returns the created program with assigned IDs for every node.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProgram([FromBody] CreateProgramRequest request)
    {
        var result = await _programService.CreateProgramAsync(request);
        if (!result.IsSuccess)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetProgram), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Return the full program tree.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProgram(Guid id)
    {
        var result = await _programService.GetProgramAsync(id);
        return ToActionResult(result);
    }

    /// <summary>
    /// Validate the program's prerequisite logic.
    /// Returns isValid, impossiblePrerequisites, and reachabilityWarnings.
    /// </summary>
    [HttpPost("{id:guid}/validate")]
    public async Task<IActionResult> ValidateProgram(Guid id)
    {
        var result = await _programService.ValidateProgramAsync(id);
        return ToActionResult(result);
    }

    /// <summary>
    /// Simulate a participant's progress through the program.
    /// Accepts choice selections and completed steps, returns completed, unlocked, and blocked statuses.
    /// </summary>
    [HttpPost("{id:guid}/simulate")]
    public async Task<IActionResult> SimulateProgram(Guid id, [FromBody] SimulateProgramRequest request)
    {
        var result = await _programService.SimulateProgramAsync(id, request);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(GeneralResult<T> result)
    {
        return result.Status switch
        {
            ResultStatus.Success => Ok(result),
            ResultStatus.NotFound => NotFound(result),
            ResultStatus.ValidationError => BadRequest(result),
            ResultStatus.Failure => BadRequest(result),
            _ => BadRequest(result)
        };
    }
}
