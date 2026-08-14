using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Profiling;

public sealed record ColumnProfile(
    Guid ColumnId,
    string ColumnName,
    DataType DataType,
    SemanticType SemanticType,
    int TotalCount,
    int EmptyCount,
    int UniqueCount,
    int DuplicateCount,
    int InvalidCount,
    decimal? Minimum,
    decimal? Maximum,
    decimal? Average);
