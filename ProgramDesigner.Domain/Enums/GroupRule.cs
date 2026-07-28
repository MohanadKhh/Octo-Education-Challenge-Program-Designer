namespace ProgramDesigner.Domain.Enums;

/// <summary>
/// The completion rule for a Group node.
/// </summary>
public enum GroupRule
{
    /// <summary>Every child must be completed in the order listed.</summary>
    InOrder,

    /// <summary>The participant picks and completes any N of the M children (N stored in <see cref="Entities.Node.ChoiceCount"/>).</summary>
    Choice
}
