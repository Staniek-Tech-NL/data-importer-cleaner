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

public sealed record ExportRequest(string FilePath, ImportedDataset Dataset);

public interface IDataFileWriter
{
    bool CanWrite(string fileExtension);

    Task WriteAsync(ExportRequest request, CancellationToken cancellationToken = default);
}
