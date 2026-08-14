using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Tests;

public sealed class DataCellTests
{
    [Fact]
    public void WithCurrentValue_PreservesSourceAndParsedValues()
    {
        var columnId = Guid.NewGuid();
        var cell = new DataCell(columnId, "€2.500,50", 2500.50m);

        var changed = cell.WithCurrentValue(2500.5m);

        Assert.Equal("€2.500,50", changed.SourceValue);
        Assert.Equal(2500.50m, changed.ParsedValue);
        Assert.Equal(2500.5m, changed.CurrentValue);
        Assert.False(changed.IsModified);
    }

    [Fact]
    public void WithDifferentCurrentValue_MarksCellAsModified()
    {
        var cell = new DataCell(Guid.NewGuid(), " NL ", " NL ");

        var changed = cell.WithCurrentValue("Netherlands");

        Assert.True(changed.IsModified);
        Assert.Equal(" NL ", changed.SourceValue);
    }
}
