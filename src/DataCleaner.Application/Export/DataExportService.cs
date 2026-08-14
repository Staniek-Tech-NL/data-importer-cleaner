using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Data;

namespace DataCleaner.Application.Export;

public sealed class DataExportService(IEnumerable<IDataFileWriter> writers) : IDataExportService
{
    public async Task<ExportResult> ExportAsync(
        ExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            throw new ArgumentException("An output file path is required.", nameof(request));
        }

        var extension = Path.GetExtension(request.FilePath);
        var writer = writers.SingleOrDefault(candidate => candidate.CanWrite(extension))
            ?? throw new NotSupportedException($"Export format '{extension}' is not supported.");
        var rows = request.Dataset.Rows.Where(row => Matches(row, request.Filter)).ToArray();
        var filtered = new ImportedDataset(request.Dataset.SourceName, request.Dataset.Columns, rows);
        await writer.WriteAsync(request with { Dataset = filtered }, cancellationToken);
        return new ExportResult(request.FilePath, rows.Length, request.Filter, DateTimeOffset.UtcNow);
    }

    private static bool Matches(ImportedRow row, ExportRowFilter filter) => filter switch
    {
        ExportRowFilter.All => true,
        ExportRowFilter.Valid => !row.State.HasFlag(RowState.Invalid) && !row.State.HasFlag(RowState.Rejected),
        ExportRowFilter.Invalid => row.State.HasFlag(RowState.Invalid) || row.State.HasFlag(RowState.Rejected),
        ExportRowFilter.Modified => row.State.HasFlag(RowState.Modified),
        _ => throw new ArgumentOutOfRangeException(nameof(filter))
    };
}
