using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Full Computer Science qualification scenario.
/// Must validate cleanly: isValid=true, 0 impossible prerequisites, 0 warnings.
/// 
/// Computer Science
/// ├── Foundations [in order]
/// │   ├── Step: Introduction to Computing
/// │   └── Step: Mathematics for Computing
/// ├── Major [choice — pick 1 of 3] · PREREQUISITE: Foundations
/// │   ├── AI [in order]
/// │   │   ├── Step: Machine Learning Basics
/// │   │   ├── Electives [choice — pick 2 of 3]
/// │   │   │   ├── Step: Computer Vision
/// │   │   │   ├── Step: Natural Language Processing
/// │   │   │   └── Step: Robotics
/// │   │   └── Step: AI Capstone · PREREQUISITE: Electives
/// │   ├── IT [in order]
/// │   │   ├── Step: Networks &amp; Security
/// │   │   └── Step: Systems Administration
/// │   └── Programming [in order]
/// │       ├── Step: Algorithms &amp; Data Structures
/// │       └── Step: Software Engineering
/// └── Step: Final Capstone · PREREQUISITE: Major
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
