using FluentAssertions;
using ProgramDesigner.Application.Services;
using ProgramDesigner.Domain.Enums;
using ProgramDesigner.Tests.Helpers;

namespace ProgramDesigner.Tests.Validation;

/// <summary>
/// Reachability Warning Tests (valid programs, conditional prerequisite warnings).
/// 
/// ── Tree Scenario 1: Nested Transitive Reachability ─────────────────────────────
/// Program
/// ├── Track [choice — pick 1 of 2]
/// │   ├── Backend [in order]
/// │   │   ├── Specialization [choice — pick 1 of 2]
/// │   │   │   ├── Step: Databases
/// │   │   │   └── Step: Distributed Systems
/// │   │   └── Step: Backend Capstone · PREREQUISITE: Distributed Systems ⚠️ [WARNING 1: Nested Choice]
/// │   └── Frontend [in order]
/// │       └── Step: Frontend Capstone
/// └── Step: Final Project · PREREQUISITE: Backend Capstone ⚠️ [WARNING 2: Cross-Track Choice]
/// 
/// 
/// ── Tree Scenario 2: Deeply Nested Curriculum (Choice &amp; InOrder) ────────────────
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
/// │           │   └── Step: NLP Advanced · PREREQUISITE: NLP Fundamentals ✅ [SAFE: sourceInSameBranch = true]
/// │           └── Branch B3: Robotics (Step)
/// └── 3. Graduation Thesis · PREREQUISITE: Specialization Track ✅ [SAFE: Choice Group As A Whole]
/// 
/// </summary>
public class ReachabilityWarningTests
{
    private readonly ProgramValidator _validator = new();

    [Fact]
    public void NestedTransitiveReachability_ProducesTwoWarnings()
    {
        // Arrange
        var distributedSystems = TreeBuilder.Step("Distributed Systems");

        var specialization = TreeBuilder.Group("Specialization", GroupRule.Choice, 1)
            .WithChild(TreeBuilder.Step("Databases"))
            .WithChild(distributedSystems);

        var backendCapstone = TreeBuilder.Step("Backend Capstone")
            .WithPrerequisite(distributedSystems);

        var backend = TreeBuilder.Group("Backend", GroupRule.InOrder)
            .WithChild(specialization)
            .WithChild(backendCapstone);

        var frontend = TreeBuilder.Group("Frontend", GroupRule.InOrder)
            .WithChild(TreeBuilder.Step("Frontend Capstone"));

        var track = TreeBuilder.Group("Track", GroupRule.Choice, 1)
            .WithChild(backend)
            .WithChild(frontend);

        var finalProject = TreeBuilder.Step("Final Project")
            .WithPrerequisite(backendCapstone);

        var root = TreeBuilder.Group("Program", GroupRule.InOrder)
            .WithChild(track)
            .WithChild(finalProject)
            .Build();

        // Act
        var result = _validator.Validate(root);

        // Assert
        result.IsValid.Should().BeTrue("warnings don't invalidate a program");
        result.ImpossiblePrerequisites.Should().BeEmpty();
        result.ReachabilityWarnings.Should().HaveCount(2,
            "Backend Capstone→Distributed Systems and Final Project→Backend Capstone are both conditional");

        // Warning 1: Backend Capstone depends on Distributed Systems (inside Specialization choice)
        result.ReachabilityWarnings.Should().Contain(w =>
            w.NodeName == "Backend Capstone" &&
            w.PrerequisiteTargetName == "Distributed Systems");

        // Warning 2: Final Project depends on Backend Capstone (inside Track choice — transitive)
        result.ReachabilityWarnings.Should().Contain(w =>
            w.NodeName == "Final Project" &&
            w.PrerequisiteTargetName == "Backend Capstone");
    }

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
