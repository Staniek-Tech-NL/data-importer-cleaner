namespace DataCleaner.Domain.Data;

public sealed record DataCell
{
    public DataCell(Guid columnId, string? sourceValue, object? parsedValue = null)
        : this(columnId, sourceValue, parsedValue, parsedValue)
    {
    }

    private DataCell(Guid columnId, string? sourceValue, object? parsedValue, object? currentValue)
    {
        ColumnId = columnId;
        SourceValue = sourceValue;
        ParsedValue = parsedValue;
        CurrentValue = currentValue;
    }

    public Guid ColumnId { get; }

    public string? SourceValue { get; }

    public object? ParsedValue { get; }

    public object? CurrentValue { get; }

    public bool IsModified => !Equals(ParsedValue, CurrentValue);

    public DataCell WithCurrentValue(object? value) =>
        new(ColumnId, SourceValue, ParsedValue, value);
}
