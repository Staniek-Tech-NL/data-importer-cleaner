using System.Globalization;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Validation;

namespace DataCleaner.Application.Processing;

public sealed class DataValidationService(IValidationEngine validationEngine) : IDataValidationService
{
    public Task<ValidationResult> ValidateAsync(
        ImportedDataset dataset,
        IEnumerable<ValidationRuleDefinition> definitions,
        ValidationPass pass,
        string cultureName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(definitions);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var rules = definitions.Select(definition => CreateRule(dataset, definition, culture)).ToArray();
        return validationEngine.ValidateAsync(dataset, rules, pass, cancellationToken);
    }

    private static IValidationRule CreateRule(
        ImportedDataset dataset,
        ValidationRuleDefinition definition,
        CultureInfo culture)
    {
        var column = dataset.Columns.FirstOrDefault(candidate => string.Equals(
            candidate.SourceName,
            definition.SourceColumn,
            StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Column '{definition.SourceColumn}' required by a validation rule was not found.");

        return definition.Kind switch
        {
            ValidationRuleKind.Required => new RequiredValidationRule(column.Id, definition.Severity),
            ValidationRuleKind.Type => new TypeValidationRule(
                column.Id,
                column.DataType,
                culture,
                definition.Severity),
            ValidationRuleKind.Email => new EmailValidationRule(column.Id, definition.Severity),
            ValidationRuleKind.Range => new RangeValidationRule(
                column.Id,
                definition.Minimum,
                definition.Maximum,
                culture,
                definition.Severity),
            ValidationRuleKind.AllowedValue => new AllowedValueValidationRule(
                column.Id,
                definition.AllowedValues,
                definition.Severity),
            ValidationRuleKind.Unique => new UniqueValidationRule(column.Id, definition.Severity),
            _ => throw new ArgumentOutOfRangeException(nameof(definition), definition.Kind, "Unknown rule kind.")
        };
    }
}
