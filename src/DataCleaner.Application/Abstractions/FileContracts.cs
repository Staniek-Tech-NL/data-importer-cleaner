using DataCleaner.Domain.Data;

namespace DataCleaner.Application.Abstractions;

public sealed record ImportRequest(string FilePath, string? WorksheetName, string CultureName);

public interface IDataFileReader
{
    bool CanRead(string fileExtension);

    Task<ImportedDataset> ReadAsync(ImportRequest request, CancellationToken cancellationToken = default);
}

public sealed record ExportRequest(string FilePath, ImportedDataset Dataset);

public interface IDataFileWriter
{
    bool CanWrite(string fileExtension);

    Task WriteAsync(ExportRequest request, CancellationToken cancellationToken = default);
}
