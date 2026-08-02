using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Self-Reference & Forward-Reference Validation Tests.
/// 
/// ── Scenario 1: Self-Reference & InOrder Forward-Reference ─────────────────────
/// Onboarding (Group, inOrder)
/// ├── Step: Sign Contract · PREREQUISITE: Sign Contract ❌ [IMPOSSIBLE: Self-Reference]
/// ├── Step: Setup Equipment · PREREQUISITE: First Day Training ❌ [IMPOSSIBLE: Forward Reference in InOrder]
/// └── Step: First Day Training
/// 
/// ── Scenario 2: Isolated Self-Reference ───────────────────────────────────────────
/// Program (Group, inOrder)
/// └── Step: Only Step · PREREQUISITE: Only Step ❌ [IMPOSSIBLE: Self-Reference]
/// </summary>
public class SelfAndForwardReferenceTests
{
    private readonly ProgramValidator _validator = new();

    [Fact]
    public void SelfReference_IsRejected()
    {
        // Arrange
        var signContract = TreeBuilder.Step("Sign Contract");
        signContract.WithPrerequisite(signContract); // self-reference

        var firstDayTraining = TreeBuilder.Step("First Day Training");
        var setupEquipment = TreeBuilder.Step("Setup Equipment")
            .WithPrerequisite(firstDayTraining); // forward-reference in InOrder

        var root = TreeBuilder.Group("Onboarding", GroupRule.InOrder)
            .WithChild(signContract)
            .WithChild(setupEquipment)
            .WithChild(firstDayTraining)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().HaveCount(2);

        result.ImpossiblePrerequisites
            .Should().Contain(i => i.NodeName == "Sign Contract" && i.Reason.Contains("self-reference"));

        result.ImpossiblePrerequisites
            .Should().Contain(i => i.NodeName == "Setup Equipment" && i.Reason.Contains("forward reference"));
    }

    [Fact]
    public void SelfReference_Isolated_IsRejected()
    {
        // Arrange — minimal self-reference case
        var step = TreeBuilder.Step("Only Step");
        step.WithPrerequisite(step);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(step)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().ContainSingle()
            .Which.Reason.Should().Contain("self-reference");
    }
}
