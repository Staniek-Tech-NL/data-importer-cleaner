using DataCleaner.Infrastructure;
using DataCleaner.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace DataCleaner.Infrastructure.Tests;

public sealed class DatabaseInitializationTests
{
    [Fact]
    public async Task Initializer_CreatesSqliteDatabase()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DataCleaner.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "test.db");
        Directory.CreateDirectory(directory);

        try
        {
            var services = new ServiceCollection();
            services.AddInfrastructure(databasePath);

            await using (var provider = services.BuildServiceProvider())
            {
                await using var scope = provider.CreateAsyncScope();
                var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

                await initializer.InitializeAsync();

                Assert.True(File.Exists(databasePath));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
