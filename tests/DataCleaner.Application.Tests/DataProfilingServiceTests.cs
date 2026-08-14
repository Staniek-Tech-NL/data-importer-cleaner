using DataCleaner.Application.Profiling;
using DataCleaner.Domain.Data;

namespace DataCleaner.Application.Tests;

public sealed class DataProfilingServiceTests
{
    [Fact]
    public void Profile_CalculatesCountsNumericStatisticsAndEmailSemanticType()
    {
        var emailColumn = new ImportedColumn(Guid.NewGuid(), 0, "Email", DataType.Text);
        var amountColumn = new ImportedColumn(Guid.NewGuid(), 1, "Amount", DataType.Decimal);
        var dataset = new ImportedDataset(
            "orders.csv",
            [emailColumn, amountColumn],
            [
                Row(2, emailColumn, "a@example.com", amountColumn, 10m),
                Row(3, emailColumn, "a@example.com", amountColumn, 20m),
                Row(4, emailColumn, "b@example.com", amountColumn, 20m),
                Row(5, emailColumn, null, amountColumn, null)
            ]);
        var service = new DataProfilingService();

        var profiles = service.Profile(dataset, "en-US");

        var email = profiles[0];
        Assert.Equal(SemanticType.Email, email.SemanticType);
        Assert.Equal(4, email.TotalCount);
        Assert.Equal(1, email.EmptyCount);
        Assert.Equal(2, email.UniqueCount);
        Assert.Equal(1, email.DuplicateCount);

        var amount = profiles[1];
        Assert.Equal(10m, amount.Minimum);
        Assert.Equal(20m, amount.Maximum);
        Assert.Equal(50m / 3m, amount.Average);
    }

    [Fact]
    public void Profile_CountsValuesThatDoNotMatchDeclaredTechnicalType()
    {
        var column = new ImportedColumn(Guid.NewGuid(), 0, "Quantity", DataType.Integer);
        var dataset = new ImportedDataset(
            "orders.csv",
            [column],
            [
                new ImportedRow(2, [new DataCell(column.Id, "12", 12L)]),
                new ImportedRow(3, [new DataCell(column.Id, "oops", "oops")])
            ]);
        var service = new DataProfilingService();

        var profile = Assert.Single(service.Profile(dataset, "en-US"));

        Assert.Equal(1, profile.InvalidCount);
    }

    private static ImportedRow Row(
        long rowNumber,
        ImportedColumn firstColumn,
        object? firstValue,
        ImportedColumn secondColumn,
        object? secondValue) =>
        new(rowNumber,
        [
            new DataCell(firstColumn.Id, firstValue?.ToString(), firstValue),
            new DataCell(secondColumn.Id, secondValue?.ToString(), secondValue)
        ]);
}
