namespace ProgramDesigner.Application.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing transactions and repository instances.
/// </summary>
public interface IUnitOfWork
{
    IProgramRepository Programs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
