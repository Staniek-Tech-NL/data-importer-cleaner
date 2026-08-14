using System.Globalization;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Data;

namespace DataCleaner.Application.Processing;

public sealed class DataCleaningService(ICleaningEngine cleaningEngine) : IDataCleaningService
{
    public async Task<CleaningRunResult> CleanAsync(
        ImportedDataset dataset,
        IEnumerable<CleaningRuleDefinition> definitions,
        string cultureName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(definitions);
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var definitionList = definitions
            .OrderBy(definition => definition.ExecutionOrder)
            .ToArray();
        var rules = definitionList
            .Select(definition => CreateRule(dataset, definition, culture))
            .ToArray();
        var result = await cleaningEngine.CleanAsync(dataset, rules, cancellationToken);
        var columns = result.Dataset.Columns.Select(column => UpdateColumn(column, definitionList)).ToArray();
        var typedDataset = new ImportedDataset(result.Dataset.SourceName, columns, result.Dataset.Rows);
        return result with { Dataset = typedDataset };
    }

    private static ICleaningRule CreateRule(
        ImportedDataset dataset,
        CleaningRuleDefinition definition,
        CultureInfo culture)
    {
        var column = dataset.Columns.FirstOrDefault(candidate => string.Equals(
            candidate.SourceName,
            definition.SourceColumn,
            StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Column '{definition.SourceColumn}' required by a cleaning rule was not found.");

        return definition.Kind switch
        {
            CleaningRuleKind.Trim => new TrimCleaningRule(column.Id),
            CleaningRuleKind.NormalizeWhitespace => new WhitespaceCleaningRule(column.Id),
            CleaningRuleKind.UpperCase => new TextCaseCleaningRule(
                column.Id,
                TextCaseNormalization.Upper,
                culture),
            CleaningRuleKind.LowerCase => new TextCaseCleaningRule(
                column.Id,
                TextCaseNormalization.Lower,
                culture),
            CleaningRuleKind.TitleCase => new TextCaseCleaningRule(
                column.Id,
                TextCaseNormalization.Title,
                culture),
            CleaningRuleKind.NormalizeEmail => new EmailCleaningRule(column.Id),
            CleaningRuleKind.NullTokens => new NullTokenCleaningRule(column.Id, definition.Values),
            CleaningRuleKind.CountryAlias => new CountryAliasCleaningRule(column.Id, definition.Aliases),
            CleaningRuleKind.NormalizeDate => new DateCleaningRule(column.Id, culture),
            CleaningRuleKind.NormalizeDecimal => new DecimalCleaningRule(column.Id, culture),
            _ => throw new ArgumentOutOfRangeException(nameof(definition), definition.Kind, "Unknown rule kind.")
        };
    }

    private static ImportedColumn UpdateColumn(
        ImportedColumn column,
        IReadOnlyCollection<CleaningRuleDefinition> definitions)
    {
        var columnRules = definitions.Where(definition => string.Equals(
            definition.SourceColumn,
            column.SourceName,
            StringComparison.OrdinalIgnoreCase));
        var dataType = column.DataType;
        var semanticType = column.SemanticType;
        foreach (var definition in columnRules)
        {
            if (definition.Kind == CleaningRuleKind.NormalizeDate)
            {
                dataType = DataType.Date;
            }
            else if (definition.Kind == CleaningRuleKind.NormalizeDecimal)
            {
                dataType = DataType.Decimal;
            }
            else if (definition.Kind == CleaningRuleKind.NormalizeEmail)
            {
                semanticType = SemanticType.Email;
            }
        }

        return column with { DataType = dataType, SemanticType = semanticType };
    }
}
