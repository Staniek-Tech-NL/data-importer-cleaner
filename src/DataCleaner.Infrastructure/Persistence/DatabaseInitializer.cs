using Microsoft.EntityFrameworkCore;

namespace DataCleaner.Infrastructure.Persistence;

internal sealed class DatabaseInitializer(DataCleanerDbContext dbContext) : IDatabaseInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
