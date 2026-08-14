using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Data;

namespace DataCleaner.Application.Import;

internal sealed class DataImportService(IEnumerable<IDataFileReader> readers) : IDataImportService
{
    private readonly IReadOnlyList<IDataFileReader> _readers = readers.ToArray();

    public Task<IReadOnlyList<string>> GetWorksheetNamesAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var reader = FindReader(filePath);
        return reader is IWorksheetFileReader worksheetReader
            ? worksheetReader.GetWorksheetNamesAsync(filePath, cancellationToken)
            : Task.FromResult<IReadOnlyList<string>>([]);
    }

    public Task<ImportedDataset> ImportAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var reader = FindReader(request.FilePath);
        return reader.ReadAsync(request, cancellationToken);
    }

    private IDataFileReader FindReader(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        var reader = _readers.FirstOrDefault(candidate => candidate.CanRead(extension));
        if (reader is null)
        {
            throw new NotSupportedException($"Files with the '{extension}' extension are not supported.");
        }

        return reader;
    }
}
