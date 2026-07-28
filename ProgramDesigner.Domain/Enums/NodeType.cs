namespace ProgramDesigner.Domain.Enums;

/// <summary>
/// Discriminates between the two node types in the program tree.
/// </summary>
public enum NodeType
{
    /// <summary>A leaf node — an atomic requirement (e.g. "attend session", "pass test").</summary>
    Step,

    /// <summary>A container node — holds an ordered list of children (Steps or nested Groups).</summary>
    Group
}
