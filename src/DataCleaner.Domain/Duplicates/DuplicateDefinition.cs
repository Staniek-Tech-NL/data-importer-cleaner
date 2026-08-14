namespace DataCleaner.Domain.Duplicates;

public enum DuplicateComparisonSource
{
    CurrentNormalizedValue = 0,
    OriginalSourceValue
}

public sealed record DuplicateDefinition
{
    public DuplicateDefinition(
        IEnumerable<Guid> keyColumnIds,
        DuplicateComparisonSource comparisonSource = DuplicateComparisonSource.CurrentNormalizedValue)
    {
        ArgumentNullException.ThrowIfNull(keyColumnIds);
        KeyColumnIds = keyColumnIds.Distinct().ToArray();

        if (KeyColumnIds.Count == 0)
        {
            throw new ArgumentException("At least one duplicate key column is required.", nameof(keyColumnIds));
        }

        ComparisonSource = comparisonSource;
    }

    public IReadOnlyList<Guid> KeyColumnIds { get; }

    public DuplicateComparisonSource ComparisonSource { get; }
}
