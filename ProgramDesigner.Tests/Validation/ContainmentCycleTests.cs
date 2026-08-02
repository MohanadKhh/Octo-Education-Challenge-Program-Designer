using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Containment Cycle Validation Tests (Ancestor depending on descendant OR descendant depending on ancestor).
/// 
/// ── Scenario 1: Parent Has Prerequisite With Direct Child ────────────────────────
/// Certification (Group, inOrder) · PREREQUISITE: Practical Exam ❌ [IMPOSSIBLE: Parent depends on child]
/// ├── Step: Study Material
/// └── Step: Practical Exam
/// 
/// ── Scenario 2: Parent Has Prerequisite With Further Child
/// Faculty of Engineering (Group, inOrder) · PREREQUISITE: Robotics Capstone ❌ [IMPOSSIBLE: Parent depends on child]
/// ├── Track A: Civil Engineering (Group, inOrder)
/// │   └── Step: Structural Design
/// └── Track B: Electrical Engineering (Group, inOrder)
///     └── Specialization: Robotics (Group, inOrder)
///         └── Step: Robotics Capstone
/// 
/// ── Scenario 3: Child Has Prerequisite With Direct Parent ───────────────────────
/// AI Track (Group, inOrder)
/// └── Step: AI Capstone · PREREQUISITE: AI Track ❌ [IMPOSSIBLE: Child depends on parent]
/// 
/// ── Scenario 4: Child Has Prerequisite With Further Parent
/// University Qualification (Group, inOrder)
/// ├── General Track (Group, inOrder)
/// │   └── Step: Orientation
/// └── Computer Science Track (Group, inOrder)
///     └── Specialization: Machine Learning (Group, inOrder) 
///         └── Step: ML Capstone · PREREQUISITE: University Qualification ❌ [IMPOSSIBLE: Child depends on parent]
/// </summary>
public class ContainmentCycleTests
{
    private readonly ProgramValidator _validator = new();

    [Fact]
    public void Scenario1_ParentHasPrerequisiteWithDirectChild_IsRejected()
    {
        // Arrange — Direct parent group depending on its direct child step
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
            .Which.Reason.Should().Contain("containment cycle");
    }

    [Fact]
    public void Scenario2_ParentHasPrerequisiteWithFurtherChild_NestedAndParallelGroups_IsRejected()
    {
        // Arrange — Top ancestor group depending on a further child step through nested & parallel groups
        var roboticsCapstone = TreeBuilder.Step("Robotics Capstone");

        var trackA = TreeBuilder.Group("Track A: Civil Engineering", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Structural Design"));

        var roboticsSpecialization = TreeBuilder.Group("Specialization: Robotics", GroupRule.InOrder)
            .WithChild(roboticsCapstone);

        var trackB = TreeBuilder.Group("Track B: Electrical Engineering", GroupRule.InOrder)
            .WithChild(roboticsSpecialization);

        var root = TreeBuilder.Group("Faculty of Engineering", GroupRule.InOrder)
            .WithPrerequisite(roboticsCapstone) // Parent depending on further child (grandchild)
            .WithChild(trackA)
            .WithChild(trackB)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().ContainSingle()
            .Which.Reason.Should().Contain("containment cycle");
    }

    [Fact]
    public void Scenario3_ChildHasPrerequisiteWithDirectParent_IsRejected()
    {
        // Arrange — Step depending directly on its immediate parent container
        var aiTrack = TreeBuilder.Group("AI Track", GroupRule.InOrder);
        var aiCapstone = TreeBuilder.Step("AI Capstone").WithPrerequisite(aiTrack);
        aiTrack.WithChild(aiCapstone);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(aiTrack)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().ContainSingle()
            .Which.Reason.Should().Contain("containment cycle");
    }

    [Fact]
    public void Scenario4_ChildHasPrerequisiteWithFurtherParent_NestedAndParallelGroups_IsRejected()
    {
        // Arrange — Deeply nested child step depending on a further top-level parent group through nested & parallel groups
        var universityQualification = TreeBuilder.Group("University Qualification", GroupRule.InOrder);

        var mlCapstone = TreeBuilder.Step("ML Capstone")
            .WithPrerequisite(universityQualification); // Child depending on further parent (grandparent)

        var generalTrack = TreeBuilder.Group("General Track", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Orientation"));

        var mlSpecialization = TreeBuilder.Group("Specialization: Machine Learning", GroupRule.InOrder)
            .WithChild(mlCapstone);

        var csTrack = TreeBuilder.Group("Computer Science Track", GroupRule.InOrder)
            .WithChild(mlSpecialization);

        universityQualification
            .WithChild(generalTrack)
            .WithChild(csTrack);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(universityQualification)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ImpossiblePrerequisites.Should().ContainSingle()
            .Which.Reason.Should().Contain("containment cycle");
    }
}
