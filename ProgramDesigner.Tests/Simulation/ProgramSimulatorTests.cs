using FluentAssertions;
using ProgramDesigner.Application.DTOs;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Simulation;

public class ProgramSimulatorTests
{
    [Fact]
    public void CsQualification_Simulation_ReturnsCorrectStatuses()
    {
        // Arrange — CS Qualification Tree
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

        var finalCapstone = TreeBuilder.Step("Final Capstone").WithPrerequisite(major);

        var root = TreeBuilder.Group("Computer Science", GroupRule.InOrder)
            .WithChild(foundations)
            .WithChild(major)
            .WithChild(finalCapstone)
            .Build();

        // Participant selections: Major -> AI, Electives -> [Computer Vision, Robotics]
        // Completed: Intro & Math
        var selectedNodeIds = new HashSet<Guid> { ai.Id, electives.Id };
        var completedStepIds = new HashSet<Guid>
        {
            foundations.Build().Children[0].Id, // Intro to Computing
            foundations.Build().Children[1].Id  // Math for Computing
        };

        // Act
        var simulator = new ProgramSimulator();
        var result = simulator.Simulate(root, selectedNodeIds, completedStepIds);

        // Assert
        // Foundations should be Completed
        result.Completed.Should().Contain(s => s.NodeName == "Foundations");

        // Major should be Unlocked (because Foundations is completed)
        result.Unlocked.Should().Contain(s => s.NodeName == "Major");

        // Final Capstone should be Blocked (because Major is not completed yet)
        result.Blocked.Should().Contain(s => s.NodeName == "Final Capstone" && s.Reason!.Contains("Major"));

        // IT and Programming branches should be filtered out (not returned)
        result.Completed.Should().NotContain(s => s.NodeName == "IT");
        result.Unlocked.Should().NotContain(s => s.NodeName == "IT");
        result.Blocked.Should().NotContain(s => s.NodeName == "IT");
    }
}
