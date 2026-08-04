using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Self-Reference & Forward-Reference Validation Tests.
/// 
/// ── Scenario 1: Self-Reference & Immediate InOrder Forward-Reference ────────────
/// Onboarding (Group, inOrder)
/// ├── Step: Sign Contract · PREREQUISITE: Sign Contract ❌ [IMPOSSIBLE: Self-Reference]
/// ├── Step: Setup Equipment · PREREQUISITE: First Day Training ❌ [IMPOSSIBLE: Forward Reference in InOrder]
/// └── Step: First Day Training
/// 
/// ── Scenario 2: Isolated Self-Reference ───────────────────────────────────────────
/// Program (Group, inOrder)
/// └── Step: Only Step · PREREQUISITE: Only Step ❌ [IMPOSSIBLE: Self-Reference]
/// 
/// ── Scenario 3: Forward-Reference Across Further Different Groups ──────────────────
/// Bachelor of Engineering (Group, inOrder)
/// ├── Year 1: Foundations (Group, inOrder)
/// │   └── Semester 1 (Group, inOrder)
/// │       └── Step: Math 101 · PREREQUISITE: Capstone Thesis ❌ [IMPOSSIBLE: Forward Reference across further groups]
/// └── Year 4: Graduation (Group, inOrder) 
///     └── Step: Capstone Thesis
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

    [Fact]
    public void ForwardReference_AcrossFurtherDifferentGroups_IsRejected()
    {
        // Arrange — Step in Year 1 depends on Step in Year 4 under an InOrder root container
        var capstoneThesis = TreeBuilder.Step("Capstone Thesis");

        var math101 = TreeBuilder.Step("Math 101")
            .WithPrerequisite(capstoneThesis); // Forward reference to step in later group

        var semester1 = TreeBuilder.Group("Semester 1", GroupRule.InOrder)
            .WithChild(math101);

        var year1Foundations = TreeBuilder.Group("Year 1: Foundations", GroupRule.InOrder)
            .WithChild(semester1);

        var year4Graduation = TreeBuilder.Group("Year 4: Graduation", GroupRule.InOrder)
            .WithChild(capstoneThesis);

        var root = TreeBuilder.Group("Bachelor of Engineering", GroupRule.InOrder)
            .WithChild(year1Foundations)
            .WithChild(year4Graduation)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().ContainSingle()
            .Which.Reason.Should().Contain("forward reference");
    }
}
