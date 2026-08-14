namespace DataCleaner.App;

public sealed record ValidationIssueViewModel(
    long RowNumber,
    string ColumnName,
    string? SourceValue,
    string Rule,
    string Severity,
    string Message);

public sealed record RejectedRowViewModel(
    long RowNumber,
    int IssueCount,
    string Values);
