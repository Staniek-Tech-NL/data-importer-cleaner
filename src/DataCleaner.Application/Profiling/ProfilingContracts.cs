using DataCleaner.Domain.Data;
using DataCleaner.Domain.Profiling;

namespace DataCleaner.Application.Profiling;

public interface IDataProfilingService
{
    IReadOnlyList<ColumnProfile> Profile(ImportedDataset dataset, string cultureName);
}
