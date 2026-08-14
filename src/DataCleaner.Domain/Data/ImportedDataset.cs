namespace DataCleaner.Domain.Data;

public sealed class ImportedDataset
{
    public ImportedDataset(
        string sourceName,
        IEnumerable<ImportedColumn> columns,
        IEnumerable<ImportedRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        SourceName = sourceName;
        Columns = columns.ToArray();
        Rows = rows.ToArray();

        if (Columns.Select(column => column.Id).Distinct().Count() != Columns.Count)
        {
            throw new ArgumentException("Column identifiers must be unique.", nameof(columns));
        }
    }

    public string SourceName { get; }

    public IReadOnlyList<ImportedColumn> Columns { get; }

    public IReadOnlyList<ImportedRow> Rows { get; }
}
