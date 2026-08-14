using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Tests;

public sealed class ImportedDatasetTests
{
    [Fact]
    public void Constructor_RejectsDuplicateColumnIdentifiers()
    {
        var id = Guid.NewGuid();
        var columns = new[]
        {
            new ImportedColumn(id, 0, "Name"),
            new ImportedColumn(id, 1, "Email")
        };

        var action = () => new ImportedDataset("customers.csv", columns, []);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Row_CanCarryMultipleFlags()
    {
        var row = new ImportedRow(2, []);

        row.AddState(RowState.Modified | RowState.Warning | RowState.Duplicate);

        Assert.True(row.State.HasFlag(RowState.Modified));
        Assert.True(row.State.HasFlag(RowState.Warning));
        Assert.True(row.State.HasFlag(RowState.Duplicate));
    }
}
