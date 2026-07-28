using Microsoft.Extensions.DependencyInjection;
using ProgramDesigner.Application.Interfaces;
using ProgramDesigner.Application.Services;

namespace ProgramDesigner.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Register Application layer services (validator, simulator, program service).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IProgramValidator, ProgramValidator>();
        services.AddSingleton<IProgramSimulator, ProgramSimulator>();
        services.AddScoped<IProgramService, ProgramService>();
        return services;
    }
}
