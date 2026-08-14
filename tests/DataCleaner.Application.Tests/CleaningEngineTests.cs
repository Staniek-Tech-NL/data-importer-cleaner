using DataCleaner.Application.Processing;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Validation;

namespace DataCleaner.Application.Tests;

public sealed class CleaningEngineTests
{
    [Fact]
    public async Task CleaningService_AppliesRulesInOrderAndPreservesValueHistory()
    {
        var nameColumn = new ImportedColumn(0, "Name");
        var countryColumn = new ImportedColumn(1, "Country");
        var amountColumn = new ImportedColumn(2, "Amount");
        var sourceRow = new ImportedRow(2,
        [
            new DataCell(nameColumn.Id, "  jANE   DOE ", "  jANE   DOE "),
            new DataCell(countryColumn.Id, "NL", "NL"),
            new DataCell(amountColumn.Id, "12,50", "12,50")
        ]);
        sourceRow.AddState(RowState.Invalid | RowState.Duplicate);
        var dataset = new ImportedDataset(
            "customers.csv",
            [nameColumn, countryColumn, amountColumn],
            [sourceRow]);
        var service = new DataCleaningService(new CleaningEngine());
        var definitions = new[]
        {
            new CleaningRuleDefinition("Name", CleaningRuleKind.Trim, 0),
            new CleaningRuleDefinition("Name", CleaningRuleKind.NormalizeWhitespace, 1),
            new CleaningRuleDefinition("Name", CleaningRuleKind.TitleCase, 2),
            new CleaningRuleDefinition(
                "Country",
                CleaningRuleKind.CountryAlias,
                3,
                aliases: new Dictionary<string, string> { ["NL"] = "Netherlands" }),
            new CleaningRuleDefinition("Amount", CleaningRuleKind.NormalizeDecimal, 4)
        };

        var result = await service.CleanAsync(dataset, definitions, "pl-PL");

        var cleanedRow = Assert.Single(result.Dataset.Rows);
        Assert.Equal("Jane Doe", cleanedRow.Cells[0].CurrentValue);
        Assert.Equal("  jANE   DOE ", cleanedRow.Cells[0].SourceValue);
        Assert.Equal("  jANE   DOE ", cleanedRow.Cells[0].ParsedValue);
        Assert.Equal("Netherlands", cleanedRow.Cells[1].CurrentValue);
        Assert.Equal(12.50m, cleanedRow.Cells[2].CurrentValue);
        Assert.Equal(DataType.Decimal, result.Dataset.Columns[2].DataType);
        Assert.True(cleanedRow.State.HasFlag(RowState.Modified));
        Assert.True(cleanedRow.State.HasFlag(RowState.Duplicate));
        Assert.False(cleanedRow.State.HasFlag(RowState.Invalid));
        Assert.Equal(5, result.Changes.Count);
        Assert.Equal([0, 1, 2, 3, 4], result.Changes.Select(change => change.ExecutionOrder));
    }

    [Fact]
    public async Task CleaningBetweenValidationPasses_CanResolveAnIssue()
    {
        var column = new ImportedColumn(0, "Country");
        var row = new ImportedRow(2, [new DataCell(column.Id, "NL", "NL")]);
        var dataset = new ImportedDataset("customers.csv", [column], [row]);
        var validationService = new DataValidationService(new ValidationEngine());
        var cleaningService = new DataCleaningService(new CleaningEngine());
        var validationRules = new[]
        {
            new ValidationRuleDefinition(
                "Country",
                ValidationRuleKind.AllowedValue,
                ValidationSeverity.Error,
                allowedValues: ["Netherlands"])
        };

        var before = await validationService.ValidateAsync(
            dataset,
            validationRules,
            ValidationPass.BeforeCleaning,
            "en-US");
        var cleaned = await cleaningService.CleanAsync(
            dataset,
            [new CleaningRuleDefinition(
                "Country",
                CleaningRuleKind.CountryAlias,
                0,
                aliases: new Dictionary<string, string> { ["NL"] = "Netherlands" })],
            "en-US");
        var after = await validationService.ValidateAsync(
            cleaned.Dataset,
            validationRules,
            ValidationPass.AfterCleaning,
            "en-US");

        Assert.Single(before.Issues);
        Assert.Empty(after.Issues);
        Assert.Equal("Netherlands", cleaned.Dataset.Rows[0].Cells[0].CurrentValue);
    }
}
