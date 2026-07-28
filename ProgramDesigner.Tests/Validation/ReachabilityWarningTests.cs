using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Tree 4 — Nested transitive reachability warnings (valid, 2 warnings).
/// 
/// Program
/// ├── Track [choice — pick 1 of 2]
/// │   ├── Backend [in order]
/// │   │   ├── Specialization [choice — pick 1 of 2]
/// │   │   │   ├── Step: Databases
/// │   │   │   └── Step: Distributed Systems
/// │   │   └── Step: Backend Capstone · PREREQUISITE: Distributed Systems
/// │   └── Frontend [in order]
/// │       └── Step: Frontend Capstone
/// └── Step: Final Project · PREREQUISITE: Backend Capstone
/// 
/// Expect isValid=true, 2 warnings:
/// 1. Backend Capstone → Distributed Systems (conditional on Specialization choice)
/// 2. Final Project → Backend Capstone (conditional transitively via Track choice)
/// </summary>
public class ReachabilityWarningTests
{
    private readonly ProgramValidator _validator = new();

    [Fact]
    public void NestedTransitiveReachability_ProducesTwoWarnings()
    {
        // Arrange
        var distributedSystems = TreeBuilder.Step("Distributed Systems");

        var specialization = TreeBuilder.Group("Specialization", GroupRule.Choice, 1)
            .WithChild(TreeBuilder.Step("Databases"))
            .WithChild(distributedSystems);

        var backendCapstone = TreeBuilder.Step("Backend Capstone")
            .WithPrerequisite(distributedSystems);

        var backend = TreeBuilder.Group("Backend", GroupRule.InOrder)
            .WithChild(specialization)
            .WithChild(backendCapstone);

        var frontend = TreeBuilder.Group("Frontend", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Frontend Capstone"));

        var track = TreeBuilder.Group("Track", GroupRule.Choice, 1)
            .WithChild(backend)
            .WithChild(frontend);

        var finalProject = TreeBuilder.Step("Final Project")
            .WithPrerequisite(backendCapstone);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(track)
            .WithChild(finalProject)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeTrue("warnings don't invalidate a program");
        result.ImpossiblePrerequisites.Should().BeEmpty();
        result.ReachabilityWarnings.Should().HaveCount(2,
            "Backend Capstone→Distributed Systems and Final Project→Backend Capstone are both conditional");

        // Warning 1: Backend Capstone depends on Distributed Systems (inside Specialization choice)
        result.ReachabilityWarnings.Should().Contain(w =>
            w.NodeName == "Backend Capstone" &&
            w.PrerequisiteTargetName == "Distributed Systems");

        // Warning 2: Final Project depends on Backend Capstone (inside Track choice — transitive)
        result.ReachabilityWarnings.Should().Contain(w =>
            w.NodeName == "Final Project" &&
            w.PrerequisiteTargetName == "Backend Capstone");
    }

    [Fact]
    public void ConditionalReachability_GeneratesWarning_NotRejection()
    {
        // A structure where a prerequisite depends on a path the participant
        // might not take generates a warning (not a rejection).
        var optionalStep = TreeBuilder.Step("Optional Step");

        var choiceGroup = TreeBuilder.Group("Choices", GroupRule.Choice, 1)
            .WithChild(optionalStep)
            .WithChild(TreeBuilder.Step("Alternative Step"));

        var dependentStep = TreeBuilder.Step("Dependent Step")
            .WithPrerequisite(optionalStep);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(choiceGroup)
            .WithChild(dependentStep)
            .Build();

        var result = _validator.Validate(root);

        result.IsValid.Should().BeTrue("conditional reachability is a warning, not a rejection");
        result.ReachabilityWarnings.Should().ContainSingle();
        result.ImpossiblePrerequisites.Should().BeEmpty();
    }
}
