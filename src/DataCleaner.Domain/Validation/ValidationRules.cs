using System.Globalization;
using System.Net.Mail;
using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Validation;

public abstract class ColumnValidationRule(
    Guid columnId,
    string code,
    ValidationSeverity severity) : IValidationRule
{
    protected Guid ColumnId { get; } = columnId;

    public string Code { get; } = code;

    public ValidationSeverity Severity { get; } = severity;

    public ValidationIssue? Validate(ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Cell.ColumnId != ColumnId ? null : ValidateCell(context);
    }

    protected abstract ValidationIssue? ValidateCell(ValidationContext context);

    protected ValidationIssue Issue(ValidationContext context, string message) => new(
        context.Row.SourceRowNumber,
        context.Cell.ColumnId,
        context.Cell.SourceValue,
        Code,
        message,
        Severity);

    protected static bool IsEmpty(object? value) =>
        value is null || value is string text && string.IsNullOrWhiteSpace(text);
}

public sealed class RequiredValidationRule(Guid columnId, ValidationSeverity severity)
    : ColumnValidationRule(columnId, "required", severity)
{
    protected override ValidationIssue? ValidateCell(ValidationContext context) =>
        IsEmpty(context.Cell.CurrentValue)
            ? Issue(context, "A value is required.")
            : null;
}

public sealed class EmailValidationRule(Guid columnId, ValidationSeverity severity)
    : ColumnValidationRule(columnId, "email", severity)
{
    protected override ValidationIssue? ValidateCell(ValidationContext context)
    {
        var value = Convert.ToString(context.Cell.CurrentValue, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return MailAddress.TryCreate(value.Trim(), out var address)
            && string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase)
                ? null
                : Issue(context, "The value is not a valid email address.");
    }
}

public sealed class TypeValidationRule(
    Guid columnId,
    DataType dataType,
    CultureInfo culture,
    ValidationSeverity severity)
    : ColumnValidationRule(columnId, "type", severity)
{
    protected override ValidationIssue? ValidateCell(ValidationContext context)
    {
        var value = context.Cell.CurrentValue;
        if (IsEmpty(value) || IsValid(value))
        {
            return null;
        }

        return Issue(context, $"The value does not match the expected {dataType} type.");
    }

    private bool IsValid(object? value) => dataType switch
    {
        DataType.Unknown => true,
        DataType.Text => value is string,
        DataType.Integer => value is sbyte or byte or short or ushort or int or uint or long
            || value is string integerText
                && long.TryParse(integerText, NumberStyles.Integer, culture, out _),
        DataType.Decimal => value is decimal or float or double or sbyte or byte or short or ushort or int or uint or long
            || value is string decimalText
                && decimal.TryParse(decimalText, NumberStyles.Number, culture, out _),
        DataType.Date => value is DateTime or DateTimeOffset
            || value is string dateText
                && DateTime.TryParse(dateText, culture, DateTimeStyles.AllowWhiteSpaces, out _),
        DataType.Boolean => value is bool
            || value is string booleanText && bool.TryParse(booleanText, out _),
        _ => false
    };
}

public sealed class RangeValidationRule(
    Guid columnId,
    decimal? minimum,
    decimal? maximum,
    CultureInfo culture,
    ValidationSeverity severity)
    : ColumnValidationRule(columnId, "range", severity)
{
    protected override ValidationIssue? ValidateCell(ValidationContext context)
    {
        if (IsEmpty(context.Cell.CurrentValue))
        {
            return null;
        }

        if (!TryConvert(context.Cell.CurrentValue, out var value))
        {
            return Issue(context, "The value is not numeric and cannot be checked against the range.");
        }

        if (minimum.HasValue && value < minimum.Value)
        {
            return Issue(context, $"The value must be at least {minimum.Value.ToString(culture)}.");
        }

        return maximum.HasValue && value > maximum.Value
            ? Issue(context, $"The value must be at most {maximum.Value.ToString(culture)}.")
            : null;
    }

    private bool TryConvert(object? value, out decimal number)
    {
        if (value is IConvertible convertible && value is not string)
        {
            try
            {
                number = convertible.ToDecimal(CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
            {
                // Fall through to the culture-aware string parser.
            }
        }

        return decimal.TryParse(
            Convert.ToString(value, culture),
            NumberStyles.Number,
            culture,
            out number);
    }
}

public sealed class AllowedValueValidationRule(
    Guid columnId,
    IEnumerable<string> allowedValues,
    ValidationSeverity severity)
    : ColumnValidationRule(columnId, "allowed-value", severity)
{
    private readonly HashSet<string> _allowedValues = new(
        allowedValues,
        StringComparer.OrdinalIgnoreCase);

    protected override ValidationIssue? ValidateCell(ValidationContext context)
    {
        var value = Convert.ToString(context.Cell.CurrentValue, CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(value) || _allowedValues.Contains(value.Trim())
            ? null
            : Issue(context, "The value is not in the allowed-value list.");
    }
}

public sealed class UniqueValidationRule(Guid columnId, ValidationSeverity severity)
    : ColumnValidationRule(columnId, "unique", severity)
{
    private IReadOnlyDictionary<string, int>? _valueCounts;

    protected override ValidationIssue? ValidateCell(ValidationContext context)
    {
        if (IsEmpty(context.Cell.CurrentValue))
        {
            return null;
        }

        _valueCounts ??= context.Dataset.Rows
            .SelectMany(row => row.Cells)
            .Where(cell => cell.ColumnId == ColumnId && !IsEmpty(cell.CurrentValue))
            .GroupBy(cell => CreateKey(cell.CurrentValue), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return _valueCounts.GetValueOrDefault(CreateKey(context.Cell.CurrentValue)) > 1
            ? Issue(context, "The value must be unique.")
            : null;
    }

    private static string CreateKey(object? value) => value switch
    {
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value?.ToString() ?? string.Empty
    };
}
