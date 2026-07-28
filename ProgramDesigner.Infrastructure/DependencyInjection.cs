using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Infrastructure.Data;
using ProgramDesigner.Infrastructure.Repositories;

namespace ProgramDesigner.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Register Infrastructure services: EF Core InMemory + repositories + application services.
    /// To switch to SQL Server, replace UseInMemoryDatabase with UseSqlServer.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("ProgramDesignerDb"));

        services.AddScoped<IProgramRepository, ProgramRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
