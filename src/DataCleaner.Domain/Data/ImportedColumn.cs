namespace DataCleaner.Domain.Data;

public sealed record ImportedColumn(
    Guid Id,
    int Index,
    string SourceName,
    DataType DataType = DataType.Unknown,
    SemanticType SemanticType = SemanticType.None)
{
    public ImportedColumn(int index, string sourceName)
        : this(Guid.NewGuid(), index, sourceName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
    }
}
