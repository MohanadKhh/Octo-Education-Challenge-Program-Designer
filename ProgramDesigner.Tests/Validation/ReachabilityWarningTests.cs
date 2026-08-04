using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Reachability Warning Tests (valid programs, conditional prerequisite warnings).
/// 
/// ── Tree Scenario 1: Basic Conditional Reachability ─────────────────────────────
/// Program (Group, inOrder)
/// ├── Choices (Group, CHOICE: 1 of 2)
/// │   ├── Step: Optional Step
/// │   └── Step: Alternative Step
/// └── Step: Dependent Step · PREREQUISITE: Optional Step ⚠️ [WARNING: Target under Choice]
/// 
/// 
/// ── Tree Scenario 2: Prerequisite Inside Choice(N = M) Group (Mandatory Selection) ──
/// Program (Group, inOrder)
/// ├── Mandatory Options (Group, CHOICE: 2 of 2) [N = M, pick all mandatory]
/// │   ├── Step: Required Module A
/// │   └── Step: Required Module B
/// └── Step: Advanced Thesis · PREREQUISITE: Required Module A ✅ [SAFE: N = M, all children mandatory]
/// 
/// 
/// ── Tree Scenario 3: Deeply Nested Curriculum (Choice & InOrder) ────────────────
/// Bachelor of Computer Science (Group, inOrder)
/// ├── 1. Core Foundations (Group, inOrder)
/// │   ├── Step: Intro to Programming
/// │   └── Step: Data Structures
/// ├── 2. Specialization Track (Group, CHOICE: 1 of 2)
/// │   ├── Branch A: Software Engineering Track (Group, inOrder)
/// │   │   ├── Step: Web Architecture
/// │   │   └── Step: Advanced Java · PREREQUISITE: Neural Networks ⚠️ [WARNING 1: Cross-Track Choice]
/// │   └── Branch B: AI Track (Group, inOrder)
/// │       ├── Step: Neural Networks
/// │       └── AI Electives (Group, CHOICE: 1 of 3)
/// │           ├── Branch B1: Computer Vision (Group, inOrder)
/// │           │   └── Step: CV Project
/// │           ├── Branch B2: NLP Track (Group, inOrder)
/// │           │   ├── Step: NLP Fundamentals
/// │           │   ├── Step: Chatbot Capstone · PREREQUISITE: CV Project ⚠️ [WARNING 2: Nested Choice]
/// │           │   └── Step: NLP Advanced · PREREQUISITE: NLP Fundamentals ✅ [SAFE: Same Branch]
/// │           └── Branch B3: Robotics (Step)
/// └── 3. Graduation Thesis · PREREQUISITE: Specialization Track ✅ [SAFE: Choice Group As A Whole]
/// 
/// </summary>
public class ReachabilityWarningTests
{
    private readonly ProgramValidator _validator = new();

    [Fact]
    public void ConditionalReachability_GeneratesWarning_NotRejection()
    {
        // A structure where a prerequisite depends on a path the participant
        // might not take generates a warning (not a rejection).
        var optionalStep = TreeBuilder.Step("Optional Step");

        var choiceGroup = TreeBuilder.Group("Choices", GroupRule.Choice, 1)
            .WithChild(optionalStep)
            .WithChild(TreeBuilder.Step("Alternative Step"));

        var dependentStep = TreeBuilder.Step("Dependent Step")
            .WithPrerequisite(optionalStep);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(choiceGroup)
            .WithChild(dependentStep)
            .Build();

        var result = _validator.Validate(root);

        result.IsValid.Should().BeTrue("conditional reachability is a warning, not a rejection");
        result.ReachabilityWarnings.Should().ContainSingle();
        result.ImpossiblePrerequisites.Should().BeEmpty();
    }

    [Fact]
    public void PrerequisiteTargetInsideChoiceGroupWhereNEqualsM_IsFullySafe_NoWarning()
    {
        // Arrange — Choice group where ChoiceCount == Children.Count (N = M, pick 2 of 2)
        var requiredModuleA = TreeBuilder.Step("Required Module A");

        var mandatoryGroup = TreeBuilder.Group("Mandatory Options", GroupRule.Choice, 2)
            .WithChild(requiredModuleA)
            .WithChild(TreeBuilder.Step("Required Module B"));

        var advancedThesis = TreeBuilder.Step("Advanced Thesis")
            .WithPrerequisite(requiredModuleA);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(mandatoryGroup)
            .WithChild(advancedThesis)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeTrue("program with mandatory choice N=M should be valid");
        result.ImpossiblePrerequisites.Should().BeEmpty();
        result.ReachabilityWarnings.Should().BeEmpty(
            "when ChoiceCount == Children.Count (N = M), every child is mandatory, so there is no reachability risk");
    }

    [Fact]
    public void DeeplyNestedCurriculum_EvaluatesReachabilityWarningsAndCoSelectionCorrectly()
    {
        // Arrange
        var neuralNetworks = TreeBuilder.Step("Neural Networks");
        var cvProject = TreeBuilder.Step("CV Project");
        var nlpFundamentals = TreeBuilder.Step("NLP Fundamentals");

        var advancedJava = TreeBuilder.Step("Advanced Java")
            .WithPrerequisite(neuralNetworks); // Warning 1: Target is inside Branch B (AI Track) choice

        var chatbotCapstone = TreeBuilder.Step("Chatbot Capstone")
            .WithPrerequisite(cvProject); // Warning 2: Target is inside Branch B1 (Computer Vision) choice

        var nlpAdvanced = TreeBuilder.Step("NLP Advanced")
            .WithPrerequisite(nlpFundamentals); // SAFE: Both are co-selected under Branch B2 (NLP Track)

        var branchA = TreeBuilder.Group("Branch A: Software Engineering Track", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Web Architecture"))
            .WithChild(advancedJava);

        var branchB1 = TreeBuilder.Group("Branch B1: Computer Vision", GroupRule.InOrder)
            .WithChild(cvProject);

        var branchB2 = TreeBuilder.Group("Branch B2: NLP Track", GroupRule.InOrder)
            .WithChild(nlpFundamentals)
            .WithChild(chatbotCapstone)
            .WithChild(nlpAdvanced);

        var aiElectives = TreeBuilder.Group("AI Electives", GroupRule.Choice, 1)
            .WithChild(branchB1)
            .WithChild(branchB2)
            .WithChild(TreeBuilder.Step("Branch B3: Robotics"));

        var branchB = TreeBuilder.Group("Branch B: AI Track", GroupRule.InOrder)
            .WithChild(neuralNetworks)
            .WithChild(aiElectives);

        var specializationTrack = TreeBuilder.Group("2. Specialization Track", GroupRule.Choice, 1)
            .WithChild(branchA)
            .WithChild(branchB);

        var graduationThesis = TreeBuilder.Step("3. Graduation Thesis")
            .WithPrerequisite(specializationTrack); // SAFE: Prerequisite is on Choice Group AS A WHOLE

        var root = TreeBuilder.Group("Bachelor of Computer Science", GroupRule.InOrder)
            .WithChild(TreeBuilder.Group("1. Core Foundations", GroupRule.InOrder)
                .WithChild(TreeBuilder.Step("Intro to Programming"))
                .WithChild(TreeBuilder.Step("Data Structures")))
            .WithChild(specializationTrack)
            .WithChild(graduationThesis)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeTrue("warnings don't invalidate a program");
        result.ImpossiblePrerequisites.Should().BeEmpty();
        result.ReachabilityWarnings.Should().HaveCount(2,
            "Advanced Java -> Neural Networks and Chatbot Capstone -> CV Project generate warnings; NLP Advanced -> NLP Fundamentals and Graduation Thesis -> Specialization Track are safe");

        // Warning 1: Advanced Java depends on Neural Networks (cross-branch choice)
        result.ReachabilityWarnings.Should().Contain(w =>
            w.NodeName == "Advanced Java" &&
            w.PrerequisiteTargetName == "Neural Networks");

        // Warning 2: Chatbot Capstone depends on CV Project (nested choice)
        result.ReachabilityWarnings.Should().Contain(w =>
            w.NodeName == "Chatbot Capstone" &&
            w.PrerequisiteTargetName == "CV Project");

        // Safe 1: NLP Advanced -> NLP Fundamentals (co-selected under same branch B2)
        result.ReachabilityWarnings.Should().NotContain(w =>
            w.NodeName == "NLP Advanced");

        // Safe 2: Graduation Thesis -> Specialization Track (prerequisite on choice group as a whole)
        result.ReachabilityWarnings.Should().NotContain(w =>
            w.NodeName == "3. Graduation Thesis");
    }
}
