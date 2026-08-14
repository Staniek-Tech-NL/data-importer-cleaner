using DataCleaner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataCleaner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? databasePath = null)
    {
        var resolvedPath = databasePath ?? DatabasePath.GetDefault();
        services.AddDbContext<DataCleanerDbContext>(options =>
            options.UseSqlite($"Data Source={resolvedPath}"));
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }
}
