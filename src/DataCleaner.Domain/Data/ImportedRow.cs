namespace DataCleaner.Domain.Data;

public sealed class ImportedRow
{
    private readonly IReadOnlyList<DataCell> _cells;

    public ImportedRow(long sourceRowNumber, IEnumerable<DataCell> cells)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceRowNumber);
        ArgumentNullException.ThrowIfNull(cells);

        SourceRowNumber = sourceRowNumber;
        _cells = cells.ToArray();
    }

    public long SourceRowNumber { get; }

    public IReadOnlyList<DataCell> Cells => _cells;

    public RowState State { get; private set; }

    public void AddState(RowState state) => State |= state;

    public void RemoveState(RowState state) => State &= ~state;
}
