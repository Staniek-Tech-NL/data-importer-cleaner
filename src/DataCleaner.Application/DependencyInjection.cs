using DataCleaner.Application.Abstractions;
using DataCleaner.Application.Import;
using DataCleaner.Application.Profiling;
using DataCleaner.Application.Processing;
using Microsoft.Extensions.DependencyInjection;

namespace DataCleaner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<IDataImportService, DataImportService>();
        services.AddTransient<IDataProfilingService, DataProfilingService>();
        services.AddTransient<IValidationEngine, ValidationEngine>();
        services.AddTransient<IDataValidationService, DataValidationService>();
        services.AddTransient<ICleaningEngine, CleaningEngine>();
        services.AddTransient<IDataCleaningService, DataCleaningService>();
        return services;
    }
}
