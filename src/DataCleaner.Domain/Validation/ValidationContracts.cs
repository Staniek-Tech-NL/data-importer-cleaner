using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Validation;

public enum ValidationSeverity
{
    Info = 0,
    Warning,
    Error
}

public sealed record ValidationIssue(
    long RowNumber,
    Guid ColumnId,
    string? SourceValue,
    string RuleCode,
    string Message,
    ValidationSeverity Severity);

public sealed record ValidationContext(ImportedDataset Dataset, ImportedRow Row, DataCell Cell);

public interface IValidationRule
{
    string Code { get; }

    ValidationSeverity Severity { get; }

    ValidationIssue? Validate(ValidationContext context);
}

public enum ValidationRuleKind
{
    Required = 0,
    Type,
    Email,
    Range,
    AllowedValue,
    Unique
}

public sealed record ValidationRuleDefinition
{
    public ValidationRuleDefinition(
        string sourceColumn,
        ValidationRuleKind kind,
        ValidationSeverity severity,
        decimal? minimum = null,
        decimal? maximum = null,
        IEnumerable<string>? allowedValues = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceColumn);
        if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value)
        {
            throw new ArgumentException("The minimum cannot be greater than the maximum.");
        }

        SourceColumn = sourceColumn.Trim();
        Kind = kind;
        Severity = severity;
        Minimum = minimum;
        Maximum = maximum;
        AllowedValues = (allowedValues ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string SourceColumn { get; }

    public ValidationRuleKind Kind { get; }

    public ValidationSeverity Severity { get; }

    public decimal? Minimum { get; }

    public decimal? Maximum { get; }

    public IReadOnlyList<string> AllowedValues { get; }
}
