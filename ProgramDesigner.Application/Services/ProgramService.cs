using ProgramDesigner.Application.Common;
using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Application.Mappings;
using ProgramDesigner.Domain.Entities;
using ProgramDesigner.Domain.Enums;

namespace ProgramDesigner.Application.Services;

/// <summary>
/// Application service orchestrating program CRUD, validation, and simulation.
/// Uses GeneralResult<T> for standardized success/error responses without throwing exceptions.
/// </summary>
public class ProgramService : IProgramService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProgramValidator _validator;
    private readonly IProgramSimulator _simulator;

    public ProgramService(
        IUnitOfWork unitOfWork,
        IProgramValidator validator,
        IProgramSimulator simulator)
    {
        _unitOfWork = unitOfWork;
        _validator = validator;
        _simulator = simulator;
    }

    /// <summary>Create a new program from a DTO tree.</summary>
    public async Task<GeneralResult<ProgramResponse>> CreateProgramAsync(CreateProgramRequest request)
    {
        if (request is null)
        {
            return GeneralResult<ProgramResponse>.ValidationFailure(
                new Dictionary<string, string[]> { { "Request", new[] { "Request body cannot be null." } } },
                "Invalid request payload.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return GeneralResult<ProgramResponse>.ValidationFailure(
                new Dictionary<string, string[]> { { "Name", new[] { "Program name is required." } } },
                "Program creation failed due to validation errors.");
        }

        if (request.RootNode is null)
        {
            return GeneralResult<ProgramResponse>.ValidationFailure(
                new Dictionary<string, string[]> { { "RootNode", new[] { "Root node is required." } } },
                "Program creation failed due to validation errors.");
        }

        var programId = Guid.NewGuid();

        if (!NodeMapper.TryToDomain(request.RootNode, programId, out var rootNode, out var mappingErrors))
        {
            return GeneralResult<ProgramResponse>.ValidationFailure(
                mappingErrors!,
                "Program creation failed due to validation errors.");
        }

        if (rootNode!.Type != NodeType.Group)
        {
            return GeneralResult<ProgramResponse>.ValidationFailure(
                new Dictionary<string, string[]> { { "RootNode", new[] { "The root node of a program must be a Group." } } },
                "Program creation failed due to validation errors.");
        }

        var program = new LearningProgram
        {
            Id = programId,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            RootNode = rootNode,
            RootNodeId = rootNode.Id
        };

        await _unitOfWork.Programs.AddAsync(program);
        await _unitOfWork.SaveChangesAsync();

        var response = NodeMapper.ToResponse(program);
        return GeneralResult<ProgramResponse>.Success(response, "Program created successfully.");
    }

    /// <summary>Retrieve a program by ID.</summary>
    public async Task<GeneralResult<ProgramResponse>> GetProgramAsync(Guid id)
    {
        var program = await _unitOfWork.Programs.GetByIdAsync(id);
        if (program is null)
        {
            return GeneralResult<ProgramResponse>.NotFound($"Program with ID '{id}' was not found.");
        }

        var response = NodeMapper.ToResponse(program);
        return GeneralResult<ProgramResponse>.Success(response);
    }

    /// <summary>Validate a program's prerequisite logic.</summary>
    public async Task<GeneralResult<ProgramValidationResult>> ValidateProgramAsync(Guid id)
    {
        var program = await _unitOfWork.Programs.GetByIdAsync(id);
        if (program is null)
        {
            return GeneralResult<ProgramValidationResult>.NotFound($"Program with ID '{id}' was not found.");
        }

        var validationResult = _validator.Validate(program.RootNode);
        return GeneralResult<ProgramValidationResult>.Success(validationResult);
    }

    /// <summary>Simulate a participant's progress through a program.</summary>
    public async Task<GeneralResult<ProgramSimulationResult>> SimulateProgramAsync(Guid id, SimulateProgramRequest request)
    {
        var program = await _unitOfWork.Programs.GetByIdAsync(id);
        if (program is null)
        {
            return GeneralResult<ProgramSimulationResult>.NotFound($"Program with ID '{id}' was not found.");
        }

        request ??= new SimulateProgramRequest();

        // Build name/ID index for resolving string inputs (IDs or names) to Node GUIDs
        var nodeIndex = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        IndexTreeNodes(program.RootNode, nodeIndex);

        var selectedNodeIds = new HashSet<Guid>();
        if (request.ChoiceSelections is not null)
        {
            foreach (var kvp in request.ChoiceSelections)
            {
                foreach (var selection in kvp.Value)
                {
                    if (nodeIndex.TryGetValue(selection, out var resolvedId))
                        selectedNodeIds.Add(resolvedId);
                    else if (Guid.TryParse(selection, out var parsedGuid))
                        selectedNodeIds.Add(parsedGuid);
                }
            }
        }

        var completedStepIds = new HashSet<Guid>();
        if (request.CompletedSteps is not null)
        {
            foreach (var item in request.CompletedSteps)
            {
                if (nodeIndex.TryGetValue(item, out var resolvedId))
                    completedStepIds.Add(resolvedId);
                else if (Guid.TryParse(item, out var parsedGuid))
                    completedStepIds.Add(parsedGuid);
            }
        }

        var simulation = _simulator.Simulate(
            program.RootNode,
            selectedNodeIds,
            completedStepIds);

        return GeneralResult<ProgramSimulationResult>.Success(simulation);
    }

    private static void IndexTreeNodes(Node node, Dictionary<string, Guid> index)
    {
        index[node.Id.ToString()] = node.Id;
        if (!string.IsNullOrWhiteSpace(node.Name))
            index[node.Name] = node.Id;

        foreach (var child in node.Children)
            IndexTreeNodes(child, index);
    }
}
