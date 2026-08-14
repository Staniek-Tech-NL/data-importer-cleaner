using DataCleaner.Domain.Data;

namespace DataCleaner.Application.Abstractions;

public sealed record ImportRequest(
    string FilePath,
    string? WorksheetName,
    string CultureName,
    string? EncodingName = null);

public interface IDataFileReader
{
    bool CanRead(string fileExtension);

    Task<ImportedDataset> ReadAsync(ImportRequest request, CancellationToken cancellationToken = default);
}

public interface IDataImportService
{
    Task<IReadOnlyList<string>> GetWorksheetNamesAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ImportedDataset> ImportAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default);
}

public interface IWorksheetFileReader : IDataFileReader
{
    Task<IReadOnlyList<string>> GetWorksheetNamesAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}

public enum ExportRowFilter
{
    All = 0,
    Valid,
    Invalid,
    Modified
}

public sealed record ExportRequest(
    string FilePath,
    ImportedDataset Dataset,
    ExportRowFilter Filter = ExportRowFilter.All);

public sealed record ExportResult(
    string FilePath,
    int ExportedRows,
    ExportRowFilter Filter,
    DateTimeOffset CompletedAtUtc);

public interface IDataFileWriter
{
    bool CanWrite(string fileExtension);

    Task WriteAsync(ExportRequest request, CancellationToken cancellationToken = default);
}

public interface IDataExportService
{
    Task<ExportResult> ExportAsync(
        ExportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ErrorReportRow(
    long SourceRowNumber,
    string ColumnName,
    string RuleCode,
    string Severity,
    string Message,
    string? SourceValue);

public interface IErrorReportWriter
{
    Task WriteAsync(
        string filePath,
        IEnumerable<ErrorReportRow> rows,
        CancellationToken cancellationToken = default);
}
