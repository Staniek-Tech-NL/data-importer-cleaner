using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DataCleaner.Infrastructure.Persistence;

public sealed class DataCleanerDbContextFactory : IDesignTimeDbContextFactory<DataCleanerDbContext>
{
    public DataCleanerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DataCleanerDbContext>()
            .UseSqlite("Data Source=datacleaner.design.db")
            .Options;

        return new DataCleanerDbContext(options);
    }
}
