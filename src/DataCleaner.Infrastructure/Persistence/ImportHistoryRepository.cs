using DataCleaner.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DataCleaner.Infrastructure.Persistence;

internal sealed class ImportHistoryRepository(DataCleanerDbContext dbContext) : IImportHistoryRepository
{
    public async Task<IReadOnlyList<ImportHistoryEntry>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        var jobs = await dbContext.ImportJobs
            .AsNoTracking()
            .Include(job => job.Result)
            .ToArrayAsync(cancellationToken);
        return jobs
            .OrderByDescending(job => job.StartedAtUtc)
            .Take(count)
            .Select(job => new ImportHistoryEntry(
                job.Id,
                job.SourceFileName,
                job.StartedAtUtc,
                job.CompletedAtUtc,
                job.ProcessedRows,
                job.InvalidRows,
                job.Status,
                job.Result == null ? 0 : job.Result.ValidRows,
                job.Result == null ? 0 : job.Result.ModifiedRows,
                job.Result == null ? 0 : job.Result.DuplicatesRemoved,
                job.Result == null ? null : job.Result.OutputFileName))
            .ToArray();
    }

    public async Task SaveAsync(ImportHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ImportJobs
            .Include(job => job.Result)
            .SingleOrDefaultAsync(job => job.Id == entry.Id, cancellationToken);
        if (entity is null)
        {
            entity = new ImportJobEntity
            {
                Id = entry.Id,
                SourceFileName = entry.SourceFileName,
                StartedAtUtc = entry.StartedAtUtc,
                Status = entry.Status
            };
            dbContext.ImportJobs.Add(entity);
        }

        entity.SourceFileName = entry.SourceFileName;
        entity.StartedAtUtc = entry.StartedAtUtc;
        entity.CompletedAtUtc = entry.CompletedAtUtc;
        entity.ProcessedRows = entry.ProcessedRows;
        entity.InvalidRows = entry.InvalidRows;
        entity.Status = entry.Status;
        entity.Result ??= new ImportResultEntity
        {
            Id = Guid.NewGuid(),
            ImportJobId = entry.Id
        };
        entity.Result.ValidRows = entry.ValidRows;
        entity.Result.ModifiedRows = entry.ModifiedRows;
        entity.Result.DuplicatesRemoved = entry.DuplicatesRemoved;
        entity.Result.OutputFileName = entry.OutputFileName;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
