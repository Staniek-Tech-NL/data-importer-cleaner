using DataCleaner.Domain.Profiles;

namespace DataCleaner.Application.Abstractions;

public interface IImportProfileRepository
{
    Task<IReadOnlyList<ImportProfile>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ImportProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveAsync(ImportProfile profile, CancellationToken cancellationToken = default);
}

public sealed record ImportHistoryEntry(
    Guid Id,
    string SourceFileName,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int ProcessedRows,
    int InvalidRows,
    string Status,
    int ValidRows = 0,
    int ModifiedRows = 0,
    int DuplicatesRemoved = 0,
    string? OutputFileName = null);

public interface IImportHistoryRepository
{
    Task<IReadOnlyList<ImportHistoryEntry>> GetRecentAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task SaveAsync(ImportHistoryEntry entry, CancellationToken cancellationToken = default);
}
