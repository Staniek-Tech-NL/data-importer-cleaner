using DataCleaner.Domain.Data;
using DataCleaner.Domain.Validation;

namespace DataCleaner.Application.Processing;

public sealed class ValidationEngine : IValidationEngine
{
    public Task<ValidationResult> ValidateAsync(
        ImportedDataset dataset,
        IEnumerable<IValidationRule> rules,
        ValidationPass pass,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(rules);
        var ruleList = rules.ToArray();
        var issues = new List<ValidationIssue>();
        var rejectedRows = new List<RejectedRowReport>();

        foreach (var row in dataset.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            row.RemoveState(RowState.Valid | RowState.Info | RowState.Warning | RowState.Invalid | RowState.Rejected);
            var rowIssues = new List<ValidationIssue>();

            foreach (var cell in row.Cells)
            {
                var context = new ValidationContext(dataset, row, cell);
                foreach (var rule in ruleList)
                {
                    var issue = rule.Validate(context);
                    if (issue is not null)
                    {
                        issues.Add(issue);
                        rowIssues.Add(issue);
                    }
                }
            }

            ApplyRowState(row, rowIssues);
            if (rowIssues.Any(issue => issue.Severity == ValidationSeverity.Error))
            {
                rejectedRows.Add(new RejectedRowReport(
                    row.SourceRowNumber,
                    row.Cells.Select(cell => cell.CurrentValue).ToArray(),
                    rowIssues.ToArray()));
            }
        }

        return Task.FromResult(new ValidationResult(
            pass,
            DateTimeOffset.UtcNow,
            issues,
            rejectedRows));
    }

    private static void ApplyRowState(ImportedRow row, IReadOnlyCollection<ValidationIssue> issues)
    {
        if (issues.Any(issue => issue.Severity == ValidationSeverity.Error))
        {
            row.AddState(RowState.Invalid | RowState.Rejected);
        }
        else
        {
            row.AddState(RowState.Valid);
        }

        if (issues.Any(issue => issue.Severity == ValidationSeverity.Warning))
        {
            row.AddState(RowState.Warning);
        }

        if (issues.Any(issue => issue.Severity == ValidationSeverity.Info))
        {
            row.AddState(RowState.Info);
        }
    }
}
