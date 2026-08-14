using System.Globalization;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Duplicates;

namespace DataCleaner.Application.Processing;

public sealed class DuplicateDetectionService : IDuplicateDetectionService
{
    public Task<DuplicateDetectionResult> DetectAsync(
        ImportedDataset dataset,
        DuplicateDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(definition);

        var knownColumns = dataset.Columns.Select(column => column.Id).ToHashSet();
        if (definition.KeyColumnIds.Any(columnId => !knownColumns.Contains(columnId)))
        {
            throw new ArgumentException("A duplicate key column does not exist in the dataset.", nameof(definition));
        }

        foreach (var row in dataset.Rows)
        {
            row.RemoveState(RowState.Duplicate);
        }

        var columnPositions = definition.KeyColumnIds
            .Select(id => dataset.Columns.Select((column, index) => (column, index))
                .Single(item => item.column.Id == id).index)
            .ToArray();
        var candidates = new Dictionary<string, List<ImportedRow>>(StringComparer.Ordinal);

        foreach (var row in dataset.Rows.OrderBy(row => row.SourceRowNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parts = columnPositions.Select(position => GetKeyPart(row.Cells[position], definition.ComparisonSource)).ToArray();
            if (parts.Any(part => part is null))
            {
                continue;
            }

            var key = string.Join("|", parts.Select(part => $"{part!.Length}:{part}"));
            if (!candidates.TryGetValue(key, out var rows))
            {
                rows = [];
                candidates.Add(key, rows);
            }

            rows.Add(row);
        }

        var groups = candidates.Values
            .Where(rows => rows.Count > 1)
            .OrderBy(rows => rows[0].SourceRowNumber)
            .Select((rows, index) => new DuplicateGroup(
                index + 1,
                rows.Select(row => row.SourceRowNumber).ToArray(),
                columnPositions.Select(position => GetDisplayValue(rows[0].Cells[position], definition.ComparisonSource)).ToArray()))
            .ToArray();

        foreach (var rowNumber in groups.SelectMany(group => group.RowNumbers))
        {
            dataset.Rows.Single(row => row.SourceRowNumber == rowNumber).AddState(RowState.Duplicate);
        }

        return Task.FromResult(new DuplicateDetectionResult(DateTimeOffset.UtcNow, groups));
    }

    public async Task<DuplicateResolutionResult> ResolveAsync(
        ImportedDataset dataset,
        DuplicateDefinition definition,
        DuplicateResolutionAction action,
        CancellationToken cancellationToken = default)
    {
        var detection = await DetectAsync(dataset, definition, cancellationToken);
        var removed = action switch
        {
            DuplicateResolutionAction.MarkForReview => [],
            DuplicateResolutionAction.KeepFirst => detection.Groups.SelectMany(group => group.RowNumbers.Skip(1)).ToArray(),
            DuplicateResolutionAction.KeepLast => detection.Groups.SelectMany(group => group.RowNumbers.Take(group.RowNumbers.Count - 1)).ToArray(),
            DuplicateResolutionAction.RemoveAll => detection.Groups.SelectMany(group => group.RowNumbers).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        var removedSet = removed.ToHashSet();
        var result = removedSet.Count == 0
            ? dataset
            : new ImportedDataset(dataset.SourceName, dataset.Columns, dataset.Rows.Where(row => !removedSet.Contains(row.SourceRowNumber)));
        return new DuplicateResolutionResult(result, detection, action, removed);
    }

    private static string? GetKeyPart(DataCell cell, DuplicateComparisonSource source)
    {
        object? value = source == DuplicateComparisonSource.OriginalSourceValue ? cell.SourceValue : cell.CurrentValue;
        if (value is null)
        {
            return null;
        }

        var normalized = value switch
        {
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized.Trim();
    }

    private static object? GetDisplayValue(DataCell cell, DuplicateComparisonSource source) =>
        source == DuplicateComparisonSource.OriginalSourceValue ? cell.SourceValue : cell.CurrentValue;
}
