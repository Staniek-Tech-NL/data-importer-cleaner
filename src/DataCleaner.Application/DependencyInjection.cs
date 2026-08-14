using DataCleaner.Application.Abstractions;
using DataCleaner.Application.Import;
using DataCleaner.Application.Profiling;
using Microsoft.Extensions.DependencyInjection;

namespace DataCleaner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<IDataImportService, DataImportService>();
        services.AddTransient<IDataProfilingService, DataProfilingService>();
        return services;
    }
}
