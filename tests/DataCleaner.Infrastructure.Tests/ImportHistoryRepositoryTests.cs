using DataCleaner.Application.Abstractions;
using DataCleaner.Infrastructure;
using DataCleaner.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace DataCleaner.Infrastructure.Tests;

public sealed class ImportHistoryRepositoryTests
{
    [Fact]
    public async Task Repository_PersistsSummaryAndUpdatesCompletedExport()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DataCleaner.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "history.db");
        Directory.CreateDirectory(directory);
        try
        {
            var services = new ServiceCollection();
            services.AddInfrastructure(path);
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
            var repository = scope.ServiceProvider.GetRequiredService<IImportHistoryRepository>();
            var id = Guid.NewGuid();
            var started = DateTimeOffset.UtcNow;

            await repository.SaveAsync(new ImportHistoryEntry(id, "input.csv", started, null, 10, 2, "Imported"));
            await repository.SaveAsync(new ImportHistoryEntry(
                id, "input.csv", started, started.AddSeconds(2), 10, 1, "Exported", 9, 4, 2, "clean.csv"));
            var restored = Assert.Single(await repository.GetRecentAsync(10));

            Assert.Equal("Exported", restored.Status);
            Assert.Equal(9, restored.ValidRows);
            Assert.Equal(4, restored.ModifiedRows);
            Assert.Equal(2, restored.DuplicatesRemoved);
            Assert.Equal("clean.csv", restored.OutputFileName);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }
}
