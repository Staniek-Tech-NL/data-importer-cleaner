namespace DataCleaner.Domain.Profiles;

public sealed record ColumnMapping
{
    public ColumnMapping(
        string sourceColumn,
        string? targetField,
        bool isIgnored = false,
        bool isDuplicateKey = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceColumn);

        SourceColumn = sourceColumn.Trim();
        TargetField = string.IsNullOrWhiteSpace(targetField) ? null : targetField.Trim();
        IsIgnored = isIgnored;
        IsDuplicateKey = isDuplicateKey;
    }

    public string SourceColumn { get; }

    public string? TargetField { get; }

    public bool IsIgnored { get; }

    public bool IsDuplicateKey { get; }
}
