using System.Text;
using DataCleaner.Application.Abstractions;

namespace DataCleaner.Infrastructure.Files;

public sealed class CsvErrorReportWriter : IErrorReportWriter
{
    public async Task WriteAsync(
        string filePath,
        IEnumerable<ErrorReportRow> rows,
        CancellationToken cancellationToken = default)
    {
        CsvDataFileWriter.EnsureNewFile(filePath);
        await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await writer.WriteLineAsync("Source row,Column,Rule,Severity,Message,Source value");
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(",",
                row.SourceRowNumber,
                ExportValueFormatter.Csv(row.ColumnName),
                ExportValueFormatter.Csv(row.RuleCode),
                ExportValueFormatter.Csv(row.Severity),
                ExportValueFormatter.Csv(row.Message),
                ExportValueFormatter.Csv(row.SourceValue)));
        }
    }
}
