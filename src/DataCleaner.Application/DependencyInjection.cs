using DataCleaner.Application.Abstractions;
using DataCleaner.Application.Import;
using Microsoft.Extensions.DependencyInjection;

namespace DataCleaner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<IDataImportService, DataImportService>();
        return services;
    }
}
