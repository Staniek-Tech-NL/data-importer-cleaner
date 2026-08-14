using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Cleaning;

public sealed record CleaningContext(ImportedDataset Dataset, ImportedRow Row, DataCell Cell);

public sealed record CleaningResult(DataCell Cell, bool Changed, string? Description = null);

public interface ICleaningRule
{
    string Code { get; }

    CleaningResult Apply(CleaningContext context);
}

public enum CleaningRuleKind
{
    Trim = 0,
    NormalizeWhitespace,
    UpperCase,
    LowerCase,
    TitleCase,
    NormalizeEmail,
    NullTokens,
    CountryAlias,
    NormalizeDate,
    NormalizeDecimal
}

public enum TextCaseNormalization
{
    None = 0,
    Upper,
    Lower,
    Title
}

public sealed record CleaningRuleDefinition
{
    public CleaningRuleDefinition(
        string sourceColumn,
        CleaningRuleKind kind,
        int executionOrder,
        IEnumerable<string>? values = null,
        IEnumerable<KeyValuePair<string, string>>? aliases = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceColumn);
        ArgumentOutOfRangeException.ThrowIfNegative(executionOrder);

        SourceColumn = sourceColumn.Trim();
        Kind = kind;
        ExecutionOrder = executionOrder;
        Values = (values ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Aliases = (aliases ?? [])
            .Where(alias => !string.IsNullOrWhiteSpace(alias.Key) && !string.IsNullOrWhiteSpace(alias.Value))
            .GroupBy(alias => alias.Key.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }

    public string SourceColumn { get; }

    public CleaningRuleKind Kind { get; }

    public int ExecutionOrder { get; }

    public IReadOnlyList<string> Values { get; }

    public IReadOnlyDictionary<string, string> Aliases { get; }
}
