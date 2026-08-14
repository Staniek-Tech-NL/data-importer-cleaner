using System.Text;
using DataCleaner.Application.Abstractions;

namespace DataCleaner.Infrastructure.Files;

public sealed class CsvDataFileWriter : IDataFileWriter
{
    public bool CanWrite(string fileExtension) =>
        string.Equals(fileExtension, ".csv", StringComparison.OrdinalIgnoreCase);

    public async Task WriteAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        EnsureNewFile(request.FilePath);
        await using var stream = new FileStream(request.FilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteLineAsync(string.Join(",", request.Dataset.Columns.Select(column =>
            ExportValueFormatter.Csv(column.SourceName))));
        foreach (var row in request.Dataset.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(",", row.Cells.Select(cell =>
                ExportValueFormatter.Csv(ExportValueFormatter.Format(cell.CurrentValue)))));
        }
    }

    internal static void EnsureNewFile(string filePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (directory is null || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException("The selected output folder does not exist.");
        }

        if (File.Exists(filePath))
        {
            throw new IOException("The output file already exists. Choose a new file name to protect existing data.");
        }
    }
}
