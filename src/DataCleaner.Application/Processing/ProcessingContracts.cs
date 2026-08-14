using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Validation;

namespace DataCleaner.Application.Processing;

public enum ValidationPass
{
    BeforeCleaning = 0,
    AfterCleaning
}

public sealed record RejectedRowReport(
    long RowNumber,
    IReadOnlyList<object?> CurrentValues,
    IReadOnlyList<ValidationIssue> Issues);

public sealed record ValidationResult(
    ValidationPass Pass,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<ValidationIssue> Issues,
    IReadOnlyList<RejectedRowReport> RejectedRows);

public interface IValidationEngine
{
    Task<ValidationResult> ValidateAsync(
        ImportedDataset dataset,
        IEnumerable<IValidationRule> rules,
        ValidationPass pass,
        CancellationToken cancellationToken = default);
}

public interface IDataValidationService
{
    Task<ValidationResult> ValidateAsync(
        ImportedDataset dataset,
        IEnumerable<ValidationRuleDefinition> definitions,
        ValidationPass pass,
        string cultureName,
        CancellationToken cancellationToken = default);
}

public interface ICleaningEngine
{
    Task<CleaningRunResult> CleanAsync(
        ImportedDataset dataset,
        IEnumerable<ICleaningRule> rules,
        CancellationToken cancellationToken = default);
}

public sealed record CleaningChange(
    long RowNumber,
    Guid ColumnId,
    string RuleCode,
    object? BeforeValue,
    object? AfterValue,
    string? Description,
    int ExecutionOrder);

public sealed record CleaningRunResult(
    ImportedDataset Dataset,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<CleaningChange> Changes);

public interface IDataCleaningService
{
    Task<CleaningRunResult> CleanAsync(
        ImportedDataset dataset,
        IEnumerable<CleaningRuleDefinition> definitions,
        string cultureName,
        CancellationToken cancellationToken = default);
}
