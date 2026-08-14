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
    Task<ImportedDataset> CleanAsync(
        ImportedDataset dataset,
        IEnumerable<ICleaningRule> rules,
        CancellationToken cancellationToken = default);
}
