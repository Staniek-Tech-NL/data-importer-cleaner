namespace DataCleaner.App;

public sealed record CleaningChangeViewModel(
    long RowNumber,
    string ColumnName,
    string Rule,
    string? BeforeValue,
    string? AfterValue,
    string? Description);
