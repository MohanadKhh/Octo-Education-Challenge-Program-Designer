using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Full Computer Science Qualification Tree (Valid curriculum, zero impossible prerequisites, zero reachability warnings).
/// 
/// ── Tree Scenario: Complete CS Qualification Curriculum ────────────────────────
/// Computer Science (Group, inOrder)
/// ├── Foundations (Group, inOrder)
/// │   ├── Step: Introduction to Computing
/// │   └── Step: Mathematics for Computing
/// ├── Major (Group, CHOICE: 1 of 3) · PREREQUISITE: Foundations ✅ [SAFE: Whole Group Prerequisite]
/// │   ├── AI (Group, inOrder)
/// │   │   ├── Step: Machine Learning Basics
/// │   │   ├── Electives (Group, CHOICE: 2 of 3)
/// │   │   │   ├── Step: Computer Vision
/// │   │   │   ├── Step: Natural Language Processing
/// │   │   │   └── Step: Robotics
/// │   │   └── Step: AI Capstone · PREREQUISITE: Electives ✅ [SAFE: Whole Group Prerequisite]
/// │   ├── IT (Group, inOrder)
/// │   │   ├── Step: Networks & Security
/// │   │   └── Step: Systems Administration
/// │   └── Programming (Group, inOrder)
/// │       ├── Step: Algorithms & Data Structures
/// │       └── Step: Software Engineering
/// └── Step: Final Capstone · PREREQUISITE: Major ✅ [SAFE: Whole Group Prerequisite]
/// 
/// Expect: IsValid = true, ImpossiblePrerequisites = 0, ReachabilityWarnings = 0.
/// </summary>
public class CsQualificationTests
{
    [Fact]
    public void CsQualification_ValidatesCleanly()
    {
        // Arrange
        var foundations = TreeBuilder.Group("Foundations", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Introduction to Computing"))
            .WithChild(TreeBuilder.Step("Mathematics for Computing"));

        var electives = TreeBuilder.Group("Electives", GroupRule.Choice, 2)
            .WithChild(TreeBuilder.Step("Computer Vision"))
            .WithChild(TreeBuilder.Step("Natural Language Processing"))
            .WithChild(TreeBuilder.Step("Robotics"));

        var ai = TreeBuilder.Group("AI", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Machine Learning Basics"))
            .WithChild(electives)
            .WithChild(TreeBuilder.Step("AI Capstone").WithPrerequisite(electives));

        var it = TreeBuilder.Group("IT", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Networks & Security"))
            .WithChild(TreeBuilder.Step("Systems Administration"));

        var programming = TreeBuilder.Group("Programming", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Algorithms & Data Structures"))
            .WithChild(TreeBuilder.Step("Software Engineering"));

        var major = TreeBuilder.Group("Major", GroupRule.Choice, 1)
            .WithPrerequisite(foundations)
            .WithChild(ai)
            .WithChild(it)
            .WithChild(programming);

        var root = TreeBuilder.Group("Computer Science", GroupRule.InOrder)
            .WithChild(foundations)
            .WithChild(major)
            .WithChild(TreeBuilder.Step("Final Capstone").WithPrerequisite(major))
            .Build();

        // Act
        var validator = new ProgramValidator();
        var result = validator.Validate(root);

        // Assert
        result.IsValid.Should().BeTrue("the CS qualification should validate cleanly");
        result.ImpossiblePrerequisites.Should().BeEmpty("there should be no impossible prerequisites");
        result.ReachabilityWarnings.Should().BeEmpty(
            "Major and Electives prerequisites target choice groups as a whole, which is safe");
    }
}
