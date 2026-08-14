using DataCleaner.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace DataCleaner.Infrastructure.Files;

public sealed class SqliteDataFileWriter : IDataFileWriter
{
    public bool CanWrite(string fileExtension) =>
        string.Equals(fileExtension, ".db", StringComparison.OrdinalIgnoreCase)
        || string.Equals(fileExtension, ".sqlite", StringComparison.OrdinalIgnoreCase);

    public async Task WriteAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        CsvDataFileWriter.EnsureNewFile(request.FilePath);
        await using var connection = new SqliteConnection($"Data Source={request.FilePath}");
        await connection.OpenAsync(cancellationToken);
        try
        {
            var names = CreateUniqueNames(request.Dataset.Columns.Select(column => column.SourceName));
            await using var create = connection.CreateCommand();
            create.CommandText = $"CREATE TABLE data ({string.Join(", ", names.Select(name => $"{Quote(name)} TEXT"))});";
            await create.ExecuteNonQueryAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            foreach (var row in request.Dataset.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var insert = connection.CreateCommand();
                insert.Transaction = (SqliteTransaction)transaction;
                insert.CommandText = $"INSERT INTO data ({string.Join(", ", names.Select(Quote))}) VALUES ({string.Join(", ", names.Select((_, index) => $"$p{index}"))});";
                for (var index = 0; index < names.Length; index++)
                {
                    insert.Parameters.AddWithValue($"$p{index}", ExportValueFormatter.Format(row.Cells[index].CurrentValue));
                }

                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await connection.CloseAsync();
            File.Delete(request.FilePath);
            throw;
        }
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string[] CreateUniqueNames(IEnumerable<string> sourceNames)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return sourceNames.Select(sourceName =>
        {
            var baseName = string.IsNullOrWhiteSpace(sourceName) ? "Column" : sourceName.Trim();
            counts.TryGetValue(baseName, out var count);
            counts[baseName] = ++count;
            return count == 1 ? baseName : $"{baseName}_{count}";
        }).ToArray();
    }
}
