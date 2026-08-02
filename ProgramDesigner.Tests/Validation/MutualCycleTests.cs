using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Prerequisite Cycle Validation Tests (Direct 2-node mutual cycles & Indirect multi-node nested group cycles).
/// 
/// ── Scenario 1: Direct 2-Node Mutual Prerequisite Cycle ─────────────────────────
/// Program (Group, CHOICE: 1 of 2)
/// ├── Module A (Group, inOrder) · PREREQUISITE: Module B ❌ [IMPOSSIBLE: Mutual Prerequisite Cycle]
/// │   └── Step: A1
/// └── Module B (Group, inOrder) · PREREQUISITE: Module A ❌ [IMPOSSIBLE: Mutual Prerequisite Cycle]
///     └── Step: B1
/// 
/// ── Scenario 2: Indirect 3-Node Cycle Spanning Deeply Nested Groups ──────────────
/// Global Curriculum (Group, CHOICE: 1 of 3)
/// ├── Track 1: Software Architecture (Group, inOrder)
/// │   └── SubTrack 1A (Group, inOrder)
/// │       └── Step: Web System Design · PREREQUISITE: Distributed Consensus ❌ [IMPOSSIBLE: Indirect Cycle]
/// ├── Track 2: Distributed Systems (Group, inOrder)
/// │   └── SubTrack 2A (Group, inOrder)
/// │       └── Step: Distributed Consensus · PREREQUISITE: Neural Networks ❌ [IMPOSSIBLE: Indirect Cycle]
/// └── Track 3: Artificial Intelligence (Group, inOrder)
///     └── SubTrack 3A (Group, inOrder)
///         └── Step: Neural Networks · PREREQUISITE: Web System Design ❌ [IMPOSSIBLE: Indirect Cycle]
/// </summary>
public class MutualCycleTests
{
    private readonly ProgramValidator _validator = new();

    [Fact]
    public void MutualCrossBranchCycle_IsRejected()
    {
        // Arrange — Direct 2-node mutual prerequisite cycle: Module A <-> Module B
        var moduleA = TreeBuilder.Group("Module A", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("A1"));

        var moduleB = TreeBuilder.Group("Module B", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("B1"));

        // Mutual prerequisite cycle: A -> B and B -> A
        moduleA.WithPrerequisite(moduleB);
        moduleB.WithPrerequisite(moduleA);

        var root = TreeBuilder.Group("Program", GroupRule.Choice)
            .WithChild(moduleA)
            .WithChild(moduleB)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse("mutual prerequisites form a cycle");
        result.ImpossiblePrerequisites.Should().NotBeEmpty("the cycle should be detected");

        result.ImpossiblePrerequisites
            .Should().Contain(i => i.NodeName == "Module A" || i.NodeName == "Module B");

        result.ImpossiblePrerequisites
            .Should().OnlyContain(i => i.Reason.Contains("cycle"));
    }

    [Fact]
    public void IndirectCycle_SpanningDeeplyNestedGroups_IsRejected()
    {
        // Arrange — 3-node indirect cycle spanning 3 nested group branches:
        // Web System Design -> Distributed Consensus -> Neural Networks -> Web System Design

        var webSystemDesign = TreeBuilder.Step("Web System Design");
        var distributedConsensus = TreeBuilder.Step("Distributed Consensus");
        var neuralNetworks = TreeBuilder.Step("Neural Networks");

        // 1. Web System Design -> Distributed Consensus
        webSystemDesign.WithPrerequisite(distributedConsensus);

        // 2. Distributed Consensus -> Neural Networks
        distributedConsensus.WithPrerequisite(neuralNetworks);

        // 3. Neural Networks -> Web System Design (completes 3-node cycle!)
        neuralNetworks.WithPrerequisite(webSystemDesign);

        var track1 = TreeBuilder.Group("Track 1: Software Architecture", GroupRule.InOrder)
            .WithChild(TreeBuilder.Group("SubTrack 1A", GroupRule.InOrder)
                .WithChild(webSystemDesign));

        var track2 = TreeBuilder.Group("Track 2: Distributed Systems", GroupRule.InOrder)
            .WithChild(TreeBuilder.Group("SubTrack 2A", GroupRule.InOrder)
                .WithChild(distributedConsensus));

        var track3 = TreeBuilder.Group("Track 3: Artificial Intelligence", GroupRule.InOrder)
            .WithChild(TreeBuilder.Group("SubTrack 3A", GroupRule.InOrder)
                .WithChild(neuralNetworks));

        var root = TreeBuilder.Group("Global Curriculum", GroupRule.Choice, 1)
            .WithChild(track1)
            .WithChild(track2)
            .WithChild(track3)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeFalse("indirect prerequisite loop forms a cycle");
        result.ImpossiblePrerequisites.Should().HaveCount(3,
            "all 3 nodes in the 3-node indirect cycle (Web System Design, Distributed Consensus, Neural Networks) should be reported");

        result.ImpossiblePrerequisites.Should().Contain(i => i.NodeName == "Web System Design" && i.PrerequisiteTargetName == "Distributed Consensus");
        result.ImpossiblePrerequisites.Should().Contain(i => i.NodeName == "Distributed Consensus" && i.PrerequisiteTargetName == "Neural Networks");
        result.ImpossiblePrerequisites.Should().Contain(i => i.NodeName == "Neural Networks" && i.PrerequisiteTargetName == "Web System Design");
    }
}
