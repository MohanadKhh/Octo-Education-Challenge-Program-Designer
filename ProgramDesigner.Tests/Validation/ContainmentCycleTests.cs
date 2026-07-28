using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Tree 2 — Group depends on its own child (rejected — containment cycle).
/// 
/// Certification [in order] · PREREQUISITE: Practical Exam
/// ├── Step: Study Material
/// └── Step: Practical Exam
/// 
/// Expect 1 impossible-prerequisite entry.
/// </summary>
public class ContainmentCycleTests
{
    private readonly ProgramValidator _validator = new();

    [Fact]
    public void GroupDependingOnOwnChild_IsRejected()
    {
        // Arrange
        var practicalExam = TreeBuilder.Step("Practical Exam");

        var root = TreeBuilder.Group("Certification", GroupRule.InOrder)
            .WithPrerequisite(practicalExam)
            .WithChild(TreeBuilder.Step("Study Material"))
            .WithChild(practicalExam)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().ContainSingle()
            .Which.Reason.Should().Contain("contained inside it");
    }

    [Fact]
    public void ChildDependingOnParentGroup_IsRejected()
    {
        // Arrange — Step depending on its parent Group (InOrder)
        var parentGroup = TreeBuilder.Group("AI", GroupRule.InOrder);
        var step = TreeBuilder.Step("AI Capstone").WithPrerequisite(parentGroup);
        parentGroup.WithChild(step);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(parentGroup)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().ContainSingle()
            .Which.Reason.Should().Contain("container that contains it");
    }

    [Fact]
    public void ChildDependingOnChoiceParentGroup_IsRejected()
    {
        // Arrange — Step depending on its parent Choice Group
        var choiceGroup = TreeBuilder.Group("Electives", GroupRule.Choice, 2);
        var nlpStep = TreeBuilder.Step("Natural Language Processing").WithPrerequisite(choiceGroup);

        choiceGroup
            .WithChild(TreeBuilder.Step("Computer Vision"))
            .WithChild(nlpStep)
            .WithChild(TreeBuilder.Step("Robotics"));

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(choiceGroup)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().ContainSingle()
            .Which.Reason.Should().Contain("container that contains it");
    }
}
