using System.Globalization;
using System.Net.Mail;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Profiling;

namespace DataCleaner.Application.Profiling;

public sealed class DataProfilingService : IDataProfilingService
{
    public IReadOnlyList<ColumnProfile> Profile(ImportedDataset dataset, string cultureName)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        var culture = CultureInfo.GetCultureInfo(cultureName);

        return dataset.Columns
            .Select((column, index) => ProfileColumn(dataset, column, index, culture))
            .ToArray();
    }

    private static ColumnProfile ProfileColumn(
        ImportedDataset dataset,
        ImportedColumn column,
        int columnIndex,
        CultureInfo culture)
    {
        var values = dataset.Rows.Select(row => row.Cells[columnIndex].CurrentValue).ToArray();
        var nonEmptyValues = values.Where(value => !IsEmpty(value)).ToArray();
        var distinctCount = nonEmptyValues
            .Select(CreateComparisonKey)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var invalidCount = nonEmptyValues.Count(value => !IsValid(value, column.DataType, culture));
        var numericValues = nonEmptyValues
            .Select(TryConvertDecimal)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
        var semanticType = DetectSemanticType(column, nonEmptyValues);

        return new ColumnProfile(
            column.Id,
            column.SourceName,
            column.DataType,
            semanticType,
            values.Length,
            values.Length - nonEmptyValues.Length,
            distinctCount,
            nonEmptyValues.Length - distinctCount,
            invalidCount,
            numericValues.Length == 0 ? null : numericValues.Min(),
            numericValues.Length == 0 ? null : numericValues.Max(),
            numericValues.Length == 0 ? null : numericValues.Average());
    }

    private static SemanticType DetectSemanticType(ImportedColumn column, object?[] values)
    {
        if (values.Length == 0)
        {
            return column.SourceName.Contains("email", StringComparison.OrdinalIgnoreCase)
                ? SemanticType.Email
                : column.SemanticType;
        }

        var validEmailCount = values.Count(value =>
            value is not null && IsEmail(Convert.ToString(value, CultureInfo.InvariantCulture)));
        return validEmailCount == values.Length
            ? SemanticType.Email
            : column.SemanticType;
    }

    private static bool IsEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !MailAddress.TryCreate(value.Trim(), out var address))
        {
            return false;
        }

        return string.Equals(address.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmpty(object? value) =>
        value is null || value is string text && string.IsNullOrWhiteSpace(text);

    private static string CreateComparisonKey(object? value) => value switch
    {
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value?.ToString() ?? string.Empty
    };

    private static bool IsValid(object? value, DataType dataType, CultureInfo culture) => dataType switch
    {
        DataType.Unknown => true,
        DataType.Text => value is string,
        DataType.Integer => value is sbyte or byte or short or ushort or int or uint or long
            || value is string integerText
                && long.TryParse(integerText, NumberStyles.Integer, culture, out _),
        DataType.Decimal => TryConvertDecimal(value).HasValue
            || value is string decimalText
                && decimal.TryParse(decimalText, NumberStyles.Number, culture, out _),
        DataType.Date => value is DateTime or DateTimeOffset
            || value is string dateText
                && DateTime.TryParse(dateText, culture, DateTimeStyles.AllowWhiteSpaces, out _),
        DataType.Boolean => value is bool
            || value is string booleanText && bool.TryParse(booleanText, out _),
        _ => false
    };

    private static decimal? TryConvertDecimal(object? value) => value switch
    {
        sbyte number => number,
        byte number => number,
        short number => number,
        ushort number => number,
        int number => number,
        uint number => number,
        long number => number,
        ulong number => number,
        float number when float.IsFinite(number) => (decimal)number,
        double number when double.IsFinite(number) => (decimal)number,
        decimal number => number,
        _ => null
    };
}
