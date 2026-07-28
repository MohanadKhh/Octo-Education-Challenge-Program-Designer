using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Infrastructure.Data;

namespace ProgramDesigner.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IProgramRepository Programs { get; }

    public UnitOfWork(AppDbContext context, IProgramRepository programs)
    {
        _context = context;
        Programs = programs;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
