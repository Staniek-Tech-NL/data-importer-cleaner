using DataCleaner.Application.Processing;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Validation;

namespace DataCleaner.Application.Tests;

public sealed class ValidationEngineTests
{
    [Fact]
    public async Task ValidationPass_UpdatesRowStatesAndBuildsRejectedRowReport()
    {
        var column = new ImportedColumn(0, "Email");
        var invalidRow = new ImportedRow(2, [new DataCell(column.Id, null)]);
        var validRow = new ImportedRow(3, [new DataCell(column.Id, "person@example.com", "person@example.com")]);
        var dataset = new ImportedDataset("customers.csv", [column], [invalidRow, validRow]);
        var service = new DataValidationService(new ValidationEngine());

        var warningResult = await service.ValidateAsync(
            dataset,
            [new ValidationRuleDefinition("Email", ValidationRuleKind.Required, ValidationSeverity.Warning)],
            ValidationPass.BeforeCleaning,
            "en-US");

        Assert.Empty(warningResult.RejectedRows);
        Assert.True(invalidRow.State.HasFlag(RowState.Valid));
        Assert.True(invalidRow.State.HasFlag(RowState.Warning));

        var errorResult = await service.ValidateAsync(
            dataset,
            [new ValidationRuleDefinition("Email", ValidationRuleKind.Required, ValidationSeverity.Error)],
            ValidationPass.AfterCleaning,
            "en-US");

        Assert.Equal(ValidationPass.AfterCleaning, errorResult.Pass);
        Assert.Single(errorResult.Issues);
        Assert.Single(errorResult.RejectedRows);
        Assert.True(invalidRow.State.HasFlag(RowState.Invalid));
        Assert.True(invalidRow.State.HasFlag(RowState.Rejected));
        Assert.False(invalidRow.State.HasFlag(RowState.Warning));
        Assert.True(validRow.State.HasFlag(RowState.Valid));
    }

    [Fact]
    public async Task ValidationService_CreatesAllConfiguredRuleKinds()
    {
        var column = new ImportedColumn(Guid.NewGuid(), 0, "Code", DataType.Text);
        var row = new ImportedRow(2, [new DataCell(column.Id, "X", "X")]);
        var dataset = new ImportedDataset("codes.csv", [column], [row]);
        var service = new DataValidationService(new ValidationEngine());
        var definitions = new[]
        {
            new ValidationRuleDefinition("Code", ValidationRuleKind.Required, ValidationSeverity.Error),
            new ValidationRuleDefinition("Code", ValidationRuleKind.Type, ValidationSeverity.Error),
            new ValidationRuleDefinition("Code", ValidationRuleKind.Email, ValidationSeverity.Info),
            new ValidationRuleDefinition("Code", ValidationRuleKind.Range, ValidationSeverity.Warning, 1, 2),
            new ValidationRuleDefinition("Code", ValidationRuleKind.AllowedValue, ValidationSeverity.Warning, allowedValues: ["A", "B"]),
            new ValidationRuleDefinition("Code", ValidationRuleKind.Unique, ValidationSeverity.Error)
        };

        var result = await service.ValidateAsync(
            dataset,
            definitions,
            ValidationPass.BeforeCleaning,
            "en-US");

        Assert.Contains(result.Issues, issue => issue.RuleCode == "email");
        Assert.Contains(result.Issues, issue => issue.RuleCode == "range");
        Assert.Contains(result.Issues, issue => issue.RuleCode == "allowed-value");
    }
}
