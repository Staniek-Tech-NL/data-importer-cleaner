using System.Globalization;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Tests;

public sealed class CleaningRuleTests
{
    [Fact]
    public void TextRules_NormalizeWhitespaceCasingAndEmailDeterministically()
    {
        var context = Context("  jANE   DOE ");
        var rules = new ICleaningRule[]
        {
            new TrimCleaningRule(context.Cell.ColumnId),
            new WhitespaceCleaningRule(context.Cell.ColumnId),
            new TextCaseCleaningRule(
                context.Cell.ColumnId,
                TextCaseNormalization.Title,
                CultureInfo.GetCultureInfo("en-US"))
        };

        var cell = context.Cell;
        foreach (var rule in rules)
        {
            cell = rule.Apply(context with { Cell = cell }).Cell;
        }

        Assert.Equal("Jane Doe", cell.CurrentValue);

        var emailContext = Context(" PERSON@EXAMPLE.COM ");
        var email = new EmailCleaningRule(emailContext.Cell.ColumnId).Apply(emailContext);
        Assert.Equal("person@example.com", email.Cell.CurrentValue);
    }

    [Fact]
    public void NullAndAliasRules_UseConfiguredCaseInsensitiveValues()
    {
        var nullContext = Context("N/A");
        var aliasContext = Context("nl");

        var nullResult = new NullTokenCleaningRule(nullContext.Cell.ColumnId, ["n/a", "-"])
            .Apply(nullContext);
        var aliasResult = new CountryAliasCleaningRule(
            aliasContext.Cell.ColumnId,
            new Dictionary<string, string> { ["NL"] = "Netherlands" })
            .Apply(aliasContext);

        Assert.Null(nullResult.Cell.CurrentValue);
        Assert.Equal("Netherlands", aliasResult.Cell.CurrentValue);
    }

    [Fact]
    public void CultureRules_ParseDateAndDecimalValues()
    {
        var dateContext = Context("14.08.2026");
        var decimalContext = Context("12,50");
        var culture = CultureInfo.GetCultureInfo("pl-PL");

        var date = new DateCleaningRule(dateContext.Cell.ColumnId, culture).Apply(dateContext);
        var number = new DecimalCleaningRule(decimalContext.Cell.ColumnId, culture).Apply(decimalContext);

        Assert.Equal(new DateTime(2026, 8, 14), date.Cell.CurrentValue);
        Assert.Equal(12.50m, number.Cell.CurrentValue);
    }

    private static CleaningContext Context(string value)
    {
        var column = new ImportedColumn(0, "Value");
        var cell = new DataCell(column.Id, value, value);
        var row = new ImportedRow(2, [cell]);
        var dataset = new ImportedDataset("test.csv", [column], [row]);
        return new CleaningContext(dataset, row, cell);
    }
}
