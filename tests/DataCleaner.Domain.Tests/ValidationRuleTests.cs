using System.Globalization;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Validation;

namespace DataCleaner.Domain.Tests;

public sealed class ValidationRuleTests
{
    [Fact]
    public void RequiredRule_RejectsWhitespace()
    {
        var context = Context("Email", "   ", "   ");
        var rule = new RequiredValidationRule(context.Cell.ColumnId, ValidationSeverity.Error);

        var issue = rule.Validate(context);

        Assert.NotNull(issue);
        Assert.Equal("required", issue.RuleCode);
    }

    [Theory]
    [InlineData("person@example.com", false)]
    [InlineData("not-an-email", true)]
    public void EmailRule_ValidatesAddress(string value, bool expectsIssue)
    {
        var context = Context("Email", value, value);
        var rule = new EmailValidationRule(context.Cell.ColumnId, ValidationSeverity.Warning);

        Assert.Equal(expectsIssue, rule.Validate(context) is not null);
    }

    [Fact]
    public void TypeAndRangeRules_ReportIncompatibleAndOutOfRangeValues()
    {
        var invalidType = Context("Amount", "oops", "oops", DataType.Decimal);
        var outOfRange = Context("Amount", "125", 125m, DataType.Decimal);
        var typeRule = new TypeValidationRule(
            invalidType.Cell.ColumnId,
            DataType.Decimal,
            CultureInfo.GetCultureInfo("en-US"),
            ValidationSeverity.Error);
        var rangeRule = new RangeValidationRule(
            outOfRange.Cell.ColumnId,
            0,
            100,
            CultureInfo.GetCultureInfo("en-US"),
            ValidationSeverity.Error);

        Assert.NotNull(typeRule.Validate(invalidType));
        Assert.NotNull(rangeRule.Validate(outOfRange));
    }

    [Fact]
    public void AllowedValueRule_IgnoresCaseAndRejectsUnknownValue()
    {
        var context = Context("Country", "France", "France");
        var rule = new AllowedValueValidationRule(
            context.Cell.ColumnId,
            ["Netherlands", "Poland"],
            ValidationSeverity.Warning);

        Assert.NotNull(rule.Validate(context));
    }

    [Fact]
    public void UniqueRule_FlagsEveryRepeatedValue()
    {
        var column = new ImportedColumn(0, "Reference");
        var rows = new[]
        {
            new ImportedRow(2, [new DataCell(column.Id, "A-1", "A-1")]),
            new ImportedRow(3, [new DataCell(column.Id, "A-1", "A-1")])
        };
        var dataset = new ImportedDataset("orders.csv", [column], rows);
        var rule = new UniqueValidationRule(column.Id, ValidationSeverity.Error);

        var issues = rows.Select(row => rule.Validate(new ValidationContext(dataset, row, row.Cells[0])));

        Assert.All(issues, Assert.NotNull);
    }

    private static ValidationContext Context(
        string columnName,
        string? sourceValue,
        object? currentValue,
        DataType dataType = DataType.Text)
    {
        var column = new ImportedColumn(Guid.NewGuid(), 0, columnName, dataType);
        var cell = new DataCell(column.Id, sourceValue, currentValue);
        var row = new ImportedRow(2, [cell]);
        var dataset = new ImportedDataset("test.csv", [column], [row]);
        return new ValidationContext(dataset, row, cell);
    }
}
