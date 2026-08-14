using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Cleaning;

public sealed record CleaningContext(ImportedDataset Dataset, ImportedRow Row, DataCell Cell);

public sealed record CleaningResult(DataCell Cell, bool Changed, string? Description = null);

public interface ICleaningRule
{
    string Code { get; }

    CleaningResult Apply(CleaningContext context);
}
