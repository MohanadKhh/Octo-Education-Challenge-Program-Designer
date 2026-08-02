using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Prerequisite on Choice Group As A Whole Validation Tests (Fully valid, zero warnings).
/// 
/// ── Tree Scenario: Prerequisite Targeting a Choice Group As A Whole ────────────
/// Program (Group, inOrder)
/// ├── Foundations (Group, inOrder)
/// │   ├── Step: Basics
/// │   └── Step: Setup
/// ├── Specialty (Group, CHOICE: 2 of 3) · PREREQUISITE: Foundations ✅ [SAFE: Whole Group Prerequisite]
/// │   ├── Step: Option X
/// │   ├── Step: Option Y
/// │   └── Step: Option Z
/// └── Step: Capstone · PREREQUISITE: Specialty ✅ [SAFE: Whole Group Prerequisite]
/// 
/// Expect: IsValid = true, ImpossiblePrerequisites = 0, ReachabilityWarnings = 0.
/// </summary>
public class FullyValidNoWarningsTests
{
    [Fact]
    public void PrerequisiteOnChoiceGroupAsWhole_IsFullySafe()
    {
        // Arrange
        var foundations = TreeBuilder.Group("Foundations", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Basics"))
            .WithChild(TreeBuilder.Step("Setup"));

        var specialty = TreeBuilder.Group("Specialty", GroupRule.Choice, 2)
            .WithPrerequisite(foundations)
            .WithChild(TreeBuilder.Step("Option X"))
            .WithChild(TreeBuilder.Step("Option Y"))
            .WithChild(TreeBuilder.Step("Option Z"));

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(foundations)
            .WithChild(specialty)
            .WithChild(TreeBuilder.Step("Capstone").WithPrerequisite(specialty))
            .Build();

        // Act
        var validator = new ProgramValidator();
        var result = validator.Validate(root);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ImpossiblePrerequisites.Should().BeEmpty();
        result.ReachabilityWarnings.Should().BeEmpty(
            "prerequisites on choice groups as a whole are safe — " +
            "completing some valid selection is guaranteed");
    }
}
