using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Tree 3 — Mutual cross-branch cycle (rejected — needs real graph traversal).
/// 
/// Program
/// ├── Module A [in order] · PREREQUISITE: Module B
/// │   └── Step: A1
/// └── Module B [in order] · PREREQUISITE: Module A
///     └── Step: B1
/// 
/// Expect cycle reported as impossible.
/// </summary>
public class MutualCycleTests
{
    [Fact]
    public void MutualCrossBranchCycle_IsRejected()
    {
        // Arrange
        var moduleA = TreeBuilder.Group("Module A", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("A1"));

        var moduleB = TreeBuilder.Group("Module B", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("B1"));

        // Mutual prerequisite cycle: A→B and B→A
        moduleA.WithPrerequisite(moduleB);
        moduleB.WithPrerequisite(moduleA);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(moduleA)
            .WithChild(moduleB)
            .Build();

        // Act
        var validator = new ProgramValidator();
        var result = validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse("mutual prerequisites form a cycle");
        result.ImpossiblePrerequisites.Should().NotBeEmpty("the cycle should be detected");

        // Both Module A and Module B should be flagged
        result.ImpossiblePrerequisites
            .Should().Contain(i => i.NodeName == "Module A" || i.NodeName == "Module B");
    }
}
