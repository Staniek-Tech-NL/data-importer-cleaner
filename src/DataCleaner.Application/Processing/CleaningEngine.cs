using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Data;

namespace DataCleaner.Application.Processing;

public sealed class CleaningEngine : ICleaningEngine
{
    public Task<CleaningRunResult> CleanAsync(
        ImportedDataset dataset,
        IEnumerable<ICleaningRule> rules,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(rules);
        var ruleList = rules.ToArray();
        var changes = new List<CleaningChange>();
        var cleanedRows = new List<ImportedRow>(dataset.Rows.Count);

        foreach (var sourceRow in dataset.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = new List<DataCell>(sourceRow.Cells.Count);

            foreach (var sourceCell in sourceRow.Cells)
            {
                var currentCell = sourceCell;
                for (var order = 0; order < ruleList.Length; order++)
                {
                    var context = new CleaningContext(dataset, sourceRow, currentCell);
                    var result = ruleList[order].Apply(context);
                    if (result.Changed)
                    {
                        changes.Add(new CleaningChange(
                            sourceRow.SourceRowNumber,
                            sourceCell.ColumnId,
                            ruleList[order].Code,
                            currentCell.CurrentValue,
                            result.Cell.CurrentValue,
                            result.Description,
                            order));
                    }

                    currentCell = result.Cell;
                }

                cells.Add(currentCell);
            }

            var cleanedRow = new ImportedRow(sourceRow.SourceRowNumber, cells);
            var preservedState = sourceRow.State
                & ~(RowState.Valid | RowState.Info | RowState.Warning | RowState.Invalid | RowState.Rejected | RowState.Modified);
            cleanedRow.AddState(preservedState);
            if (cells.Any(cell => cell.IsModified))
            {
                cleanedRow.AddState(RowState.Modified);
            }

            cleanedRows.Add(cleanedRow);
        }

        var cleanedDataset = new ImportedDataset(dataset.SourceName, dataset.Columns, cleanedRows);
        return Task.FromResult(new CleaningRunResult(
            cleanedDataset,
            DateTimeOffset.UtcNow,
            changes));
    }
}
