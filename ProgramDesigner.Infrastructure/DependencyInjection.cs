using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Infrastructure.Data;
using ProgramDesigner.Infrastructure.Repositories;

namespace ProgramDesigner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        // ── Option 1: In-Memory Database (Default - Zero Setup) ──────────────────
        // Note: Data is temporary and resets when the application stops.
        // ────────────────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("ProgramDesignerDb"));


        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        // ── Option 2: Persistent Relational Database (Optional) ─────────────────
        // To connect to a real database (SQL Server, PostgreSQL, SQLite, etc.):
        // 1. Comment out Option 1 above
        // 2. Install your EF Core provider package if you don't use SQLServer as it already installed.
        // 2. uncomment the lines (32 ~ 34) and paste your connection string.
        // 3. Create & apply EF Core migrations using package Manager console and choose ProgramDesigner.Infrastructure as the default project:
        //      3.1. add-migration InitialCreate
        //      3.2. update-database
        // ───────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
        //var connectionString = "Server=.\\MOHANADKHH;DataBase=ProgramDesignerDb;Trusted_Connection=true;TrustServerCertificate=true";
        //services.AddDbContext<AppDbContext>(options =>
        //    options.UseSqlServer(connectionString));

        services.AddScoped<IProgramRepository, ProgramRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }
}
