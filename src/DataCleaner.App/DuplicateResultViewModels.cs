namespace DataCleaner.App;

public sealed record DuplicateGroupViewModel(
    int GroupNumber,
    string RowNumbers,
    string KeyValues,
    int RowCount);
