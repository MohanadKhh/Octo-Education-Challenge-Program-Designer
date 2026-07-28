using FluentAssertions;
using ProgramDesigner.Application.Common;
using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Entities;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Services;

public class ProgramServiceTests
{
    private readonly IProgramService _service;
    private readonly MemoryProgramRepository _repository;

    public ProgramServiceTests()
    {
        _repository = new MemoryProgramRepository();
        var unitOfWork = new MemoryUnitOfWork(_repository);
        var validator = new ProgramValidator();
        var simulator = new ProgramSimulator();

        _service = new ProgramService(unitOfWork, validator, simulator);
    }

    [Fact]
    public async Task CreateProgram_WithValidGroupRoot_ReturnsSuccessResult()
    {
        // Arrange
        var step1Id = Guid.NewGuid();
        var step2Id = Guid.NewGuid();

        var request = new CreateProgramRequest
        {
            Name = "Test Program",
            RootNode = new NodeDto
            {
                Name = "Root Group",
                Type = "group",
                Rule = "inOrder",
                Children = new List<NodeDto>
                {
                    new() { Id = step1Id, Name = "Step 1", Type = "step", StepType = "attend session" },
                    new() { Id = step2Id, Name = "Step 2", Type = "step", StepType = "pass test", PrerequisiteId = step1Id }
                }
            }
        };

        // Act
        var result = await _service.CreateProgramAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(ResultStatus.Success);
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Test Program");

        var step1 = result.Data.RootNode.Children![0];
        var step2 = result.Data.RootNode.Children![1];

        // Step nodes have null children (omitted in JSON output)
        step1.Children.Should().BeNull();
        step2.Children.Should().BeNull();

        // Step 2 has both PrerequisiteId and PrerequisiteName populated in response output
        step2.PrerequisiteId.Should().Be(step1.Id);
        step2.PrerequisiteName.Should().Be("Step 1");
    }

    [Fact]
    public async Task CreateProgram_WithInvalidPrerequisiteId_ReturnsValidationFailure()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        var request = new CreateProgramRequest
        {
            Name = "Invalid Prereq Program",
            RootNode = new NodeDto
            {
                Name = "Root Group",
                Type = "group",
                Rule = "inOrder",
                Children = new List<NodeDto>
                {
                    new() { Name = "Step 1", Type = "step", StepType = "attend session", PrerequisiteId = unknownId }
                }
            }
        };

        // Act
        var result = await _service.CreateProgramAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.ValidationError);
        result.Errors.Should().ContainKey("NodeMapping");
        result.Errors!["NodeMapping"].Should().Contain(e => e.Contains("no node with that ID exists"));
    }

    [Fact]
    public async Task CreateProgram_WithNonGroupRoot_ReturnsValidationFailure()
    {
        // Arrange
        var request = new CreateProgramRequest
        {
            Name = "Invalid Program",
            RootNode = new NodeDto
            {
                Name = "Root Step",
                Type = "step"
            }
        };

        // Act
        var result = await _service.CreateProgramAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.ValidationError);
        result.Errors.Should().ContainKey("RootNode");
    }

    [Fact]
    public async Task GetProgram_WithNonExistentId_ReturnsNotFoundResult()
    {
        // Act
        var result = await _service.GetProgramAsync(Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateProgram_WithInvalidNodeType_ReturnsValidationFailure_NoException()
    {
        // Arrange
        var request = new CreateProgramRequest
        {
            Name = "Invalid Type Program",
            RootNode = new NodeDto
            {
                Name = "Root",
                Type = "unknown_type"
            }
        };

        // Act
        var result = await _service.CreateProgramAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.ValidationError);
        result.Errors.Should().ContainKey("NodeMapping");
    }

    [Fact]
    public async Task CreateProgram_WithStepNodeHavingChildren_ReturnsValidationFailure()
    {
        // Arrange
        var request = new CreateProgramRequest
        {
            Name = "Invalid Step Children Program",
            RootNode = new NodeDto
            {
                Name = "Root Group",
                Type = "group",
                Rule = "inOrder",
                Children = new List<NodeDto>
                {
                    new()
                    {
                        Name = "Networks & Security",
                        Type = "step",
                        StepType = "pass test",
                        Children = new List<NodeDto>
                        {
                            new() { Name = "Child Step", Type = "step", StepType = "submit work" }
                        }
                    }
                }
            }
        };

        // Act
        var result = await _service.CreateProgramAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.ValidationError);
        result.Errors.Should().ContainKey("NodeMapping");
        result.Errors!["NodeMapping"].Should().Contain(e => e.Contains("Step node 'Networks & Security' cannot have children"));
    }
    [Fact]
    public async Task CreateProgram_WithDuplicateDatabaseKey_ReturnsValidationFailure_NoException()
    {
        // Arrange
        var rootId = Guid.NewGuid();

        var request = new CreateProgramRequest
        {
            Name = "Program 1",
            RootNode = new NodeDto
            {
                Id = rootId,
                Name = "Root Group",
                Type = "group",
                Rule = "inOrder"
            }
        };

        // First call succeeds
        var firstResult = await _service.CreateProgramAsync(request);
        firstResult.IsSuccess.Should().BeTrue();

        // Second call with same rootId triggers DB duplicate key
        var duplicateResult = await _service.CreateProgramAsync(request);

        // Assert
        duplicateResult.IsSuccess.Should().BeFalse();
        duplicateResult.Status.Should().Be(ResultStatus.ValidationError);
        duplicateResult.Errors.Should().ContainKey("EntityId");
    }
}

internal class MemoryProgramRepository : IProgramRepository
{
    private readonly Dictionary<Guid, LearningProgram> _programs = new();
    private readonly HashSet<Guid> _nodeIds = new();

    public Task AddAsync(LearningProgram program)
    {
        if (!_programs.TryAdd(program.Id, program) || !_nodeIds.Add(program.RootNode.Id))
        {
            throw new InvalidOperationException($"An item with the same key has already been added. Key: {program.RootNode.Id}");
        }
        return Task.CompletedTask;
    }

    public Task<LearningProgram?> GetByIdAsync(Guid id)
    {
        _programs.TryGetValue(id, out var p);
        return Task.FromResult(p);
    }
}

internal class MemoryUnitOfWork : IUnitOfWork
{
    public IProgramRepository Programs { get; }
    public MemoryUnitOfWork(IProgramRepository repo) => Programs = repo;
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}
