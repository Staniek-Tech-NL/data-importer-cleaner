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
