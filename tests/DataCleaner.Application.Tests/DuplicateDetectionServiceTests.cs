using DataCleaner.Application.Processing;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Duplicates;

namespace DataCleaner.Application.Tests;

public sealed class DuplicateDetectionServiceTests
{
    [Fact]
    public async Task DetectAsync_FindsSingleAndCompositeExactNormalizedKeys()
    {
        var email = new ImportedColumn(0, "Email");
        var country = new ImportedColumn(1, "Country");
        var dataset = Dataset(email, country,
            (2, " a@example.com ", "NL"),
            (3, "a@example.com", "NL"),
            (4, "a@example.com", "BE"),
            (5, null, "NL"));
        var service = new DuplicateDetectionService();

        var single = await service.DetectAsync(dataset, new DuplicateDefinition([email.Id]));
        var composite = await service.DetectAsync(dataset, new DuplicateDefinition([email.Id, country.Id]));

        Assert.Equal(3, Assert.Single(single.Groups).RowNumbers.Count);
        Assert.Equal([2L, 3L], Assert.Single(composite.Groups).RowNumbers);
        Assert.False(dataset.Rows.Single(row => row.SourceRowNumber == 5).State.HasFlag(RowState.Duplicate));
    }

    [Theory]
    [InlineData(DuplicateResolutionAction.KeepFirst, 2, 3)]
    [InlineData(DuplicateResolutionAction.KeepLast, 3, 2)]
    public async Task ResolveAsync_KeepsTheRequestedDeterministicOccurrence(
        DuplicateResolutionAction action,
        long keptRow,
        long removedRow)
    {
        var key = new ImportedColumn(0, "Key");
        var dataset = Dataset(key, null, (3, "same", null), (2, "same", null), (4, "other", null));
        dataset.Rows[0].AddState(RowState.Modified | RowState.Warning);

        var result = await new DuplicateDetectionService().ResolveAsync(
            dataset,
            new DuplicateDefinition([key.Id]),
            action);

        Assert.Contains(result.Dataset.Rows, row => row.SourceRowNumber == keptRow);
        Assert.Contains(removedRow, result.RemovedRowNumbers);
        Assert.Contains(result.Detection.Groups.Single().RowNumbers, row => row == 2);
        Assert.True(dataset.Rows[0].State.HasFlag(RowState.Modified));
        Assert.True(dataset.Rows[0].State.HasFlag(RowState.Warning));
        Assert.True(dataset.Rows[0].State.HasFlag(RowState.Duplicate));
    }

    [Fact]
    public async Task ResolveAsync_RemoveAllRemovesEveryMemberButRetainsAuditGroup()
    {
        var key = new ImportedColumn(0, "Key");
        var dataset = Dataset(key, null, (2, "x", null), (3, "x", null), (4, "y", null));

        var result = await new DuplicateDetectionService().ResolveAsync(
            dataset,
            new DuplicateDefinition([key.Id]),
            DuplicateResolutionAction.RemoveAll);

        Assert.Equal([2L, 3L], result.RemovedRowNumbers);
        Assert.Equal(4, Assert.Single(result.Dataset.Rows).SourceRowNumber);
        Assert.Equal([2L, 3L], Assert.Single(result.Detection.Groups).RowNumbers);
    }

    [Fact]
    public async Task DetectAsync_CanCompareOriginalSourceInsteadOfCurrentValue()
    {
        var key = new ImportedColumn(0, "Key");
        var first = new DataCell(key.Id, "A", "A").WithCurrentValue("same");
        var second = new DataCell(key.Id, "B", "B").WithCurrentValue("same");
        var dataset = new ImportedDataset("source.csv", [key],
            [new ImportedRow(2, [first]), new ImportedRow(3, [second])]);
        var service = new DuplicateDetectionService();

        var normalized = await service.DetectAsync(dataset, new DuplicateDefinition([key.Id]));
        var original = await service.DetectAsync(dataset, new DuplicateDefinition(
            [key.Id], DuplicateComparisonSource.OriginalSourceValue));

        Assert.Single(normalized.Groups);
        Assert.Empty(original.Groups);
    }

    private static ImportedDataset Dataset(
        ImportedColumn first,
        ImportedColumn? second,
        params (long Row, string? First, string? Second)[] values) =>
        new("source.csv", second is null ? [first] : [first, second], values.Select(value =>
            new ImportedRow(value.Row, second is null
                ? [new DataCell(first.Id, value.First, value.First)]
                : [new DataCell(first.Id, value.First, value.First), new DataCell(second.Id, value.Second, value.Second)])));
}
