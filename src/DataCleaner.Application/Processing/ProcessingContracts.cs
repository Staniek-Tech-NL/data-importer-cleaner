using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Validation;

namespace DataCleaner.Application.Processing;

public enum ValidationPass
{
    BeforeCleaning = 0,
    AfterCleaning
}

public interface IValidationEngine
{
    Task<IReadOnlyList<ValidationIssue>> ValidateAsync(
        ImportedDataset dataset,
        IEnumerable<IValidationRule> rules,
        ValidationPass pass,
        CancellationToken cancellationToken = default);
}

public interface ICleaningEngine
{
    Task<ImportedDataset> CleanAsync(
        ImportedDataset dataset,
        IEnumerable<ICleaningRule> rules,
        CancellationToken cancellationToken = default);
}
