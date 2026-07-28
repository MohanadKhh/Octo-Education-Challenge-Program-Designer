using Microsoft.EntityFrameworkCore;
using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Domain.Entities;
using ProgramDesigner.Infrastructure.Data;

namespace ProgramDesigner.Infrastructure.Repositories;

public class ProgramRepository : IProgramRepository
{
    private readonly AppDbContext _context;

    public ProgramRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LearningProgram program)
    {
        await _context.Programs.AddAsync(program);
    }

    public async Task<LearningProgram?> GetByIdAsync(Guid id)
    {
        var program = await _context.Programs
            .Include(p => p.RootNode)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (program is null) return null;

        // Eagerly load the full node tree (recursive)
        await LoadChildrenRecursivelyAsync(program.RootNode);

        return program;
    }

    /// <summary>
    /// Recursively load all children of a node.
    /// EF Core InMemory doesn't support recursive includes, so we load level by level.
    /// </summary>
    private async Task LoadChildrenRecursivelyAsync(Node node)
    {
        await _context.Entry(node)
            .Collection(n => n.Children)
            .LoadAsync();

        // Sort by Order to preserve position
        var orderedChildren = node.Children.OrderBy(c => c.Order).ToList();
        node.Children = orderedChildren;

        foreach (var child in node.Children)
        {
            await LoadChildrenRecursivelyAsync(child);
        }
    }
}
