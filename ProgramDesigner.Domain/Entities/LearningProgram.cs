namespace ProgramDesigner.Domain.Entities;

/// <summary>
/// Aggregate root representing a learning program.
/// A program is essentially a named wrapper around its root <see cref="Node"/> (always a Group).
/// </summary>
public class LearningProgram
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name of the program (e.g. "Computer Science").</summary>
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Root node ID (FK for EF Core).</summary>
    public Guid RootNodeId { get; set; }

    /// Navigational property.
    public Node RootNode { get; set; } = null!;
}
