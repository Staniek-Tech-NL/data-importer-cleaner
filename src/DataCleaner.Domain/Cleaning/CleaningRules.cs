using System.Globalization;
using System.Text.RegularExpressions;
using DataCleaner.Domain.Data;

namespace DataCleaner.Domain.Cleaning;

public abstract class ColumnCleaningRule(Guid columnId, string code) : ICleaningRule
{
    protected Guid ColumnId { get; } = columnId;

    public string Code { get; } = code;

    public CleaningResult Apply(CleaningContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Cell.ColumnId == ColumnId
            ? ApplyToCell(context)
            : new CleaningResult(context.Cell, false);
    }

    protected abstract CleaningResult ApplyToCell(CleaningContext context);

    protected static CleaningResult Replace(CleaningContext context, object? value, string description)
    {
        if (Equals(context.Cell.CurrentValue, value))
        {
            return new CleaningResult(context.Cell, false);
        }

        return new CleaningResult(context.Cell.WithCurrentValue(value), true, description);
    }
}

public sealed class TrimCleaningRule(Guid columnId) : ColumnCleaningRule(columnId, "trim")
{
    protected override CleaningResult ApplyToCell(CleaningContext context) =>
        context.Cell.CurrentValue is string value
            ? Replace(context, value.Trim(), "Trimmed leading and trailing whitespace.")
            : new CleaningResult(context.Cell, false);
}

public sealed partial class WhitespaceCleaningRule(Guid columnId)
    : ColumnCleaningRule(columnId, "normalize-whitespace")
{
    protected override CleaningResult ApplyToCell(CleaningContext context) =>
        context.Cell.CurrentValue is string value
            ? Replace(context, WhitespaceRegex().Replace(value, " "), "Normalized internal whitespace.")
            : new CleaningResult(context.Cell, false);

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}

public sealed class TextCaseCleaningRule(
    Guid columnId,
    TextCaseNormalization normalization,
    CultureInfo culture)
    : ColumnCleaningRule(columnId, normalization switch
    {
        TextCaseNormalization.Upper => "upper-case",
        TextCaseNormalization.Lower => "lower-case",
        TextCaseNormalization.Title => "title-case",
        _ => "case-none"
    })
{
    protected override CleaningResult ApplyToCell(CleaningContext context)
    {
        if (context.Cell.CurrentValue is not string value || normalization == TextCaseNormalization.None)
        {
            return new CleaningResult(context.Cell, false);
        }

        var normalized = normalization switch
        {
            TextCaseNormalization.Upper => value.ToUpper(culture),
            TextCaseNormalization.Lower => value.ToLower(culture),
            TextCaseNormalization.Title => culture.TextInfo.ToTitleCase(value.ToLower(culture)),
            _ => value
        };
        return Replace(context, normalized, $"Applied {normalization} casing.");
    }
}

public sealed class EmailCleaningRule(Guid columnId) : ColumnCleaningRule(columnId, "normalize-email")
{
    protected override CleaningResult ApplyToCell(CleaningContext context) =>
        context.Cell.CurrentValue is string value
            ? Replace(context, value.Trim().ToLowerInvariant(), "Normalized email casing and whitespace.")
            : new CleaningResult(context.Cell, false);
}

public sealed class NullTokenCleaningRule(
    Guid columnId,
    IEnumerable<string> nullTokens)
    : ColumnCleaningRule(columnId, "null-tokens")
{
    private readonly HashSet<string> _nullTokens = new(nullTokens, StringComparer.OrdinalIgnoreCase);

    protected override CleaningResult ApplyToCell(CleaningContext context) =>
        context.Cell.CurrentValue is string value && _nullTokens.Contains(value.Trim())
            ? Replace(context, null, "Replaced a configured null token with an empty value.")
            : new CleaningResult(context.Cell, false);
}

public sealed class CountryAliasCleaningRule(
    Guid columnId,
    IEnumerable<KeyValuePair<string, string>> aliases)
    : ColumnCleaningRule(columnId, "country-alias")
{
    private readonly Dictionary<string, string> _aliases = aliases.ToDictionary(
        alias => alias.Key,
        alias => alias.Value,
        StringComparer.OrdinalIgnoreCase);

    protected override CleaningResult ApplyToCell(CleaningContext context)
    {
        if (context.Cell.CurrentValue is not string value
            || !_aliases.TryGetValue(value.Trim(), out var canonicalValue))
        {
            return new CleaningResult(context.Cell, false);
        }

        return Replace(context, canonicalValue, "Replaced a country alias with its canonical value.");
    }
}

public sealed class DateCleaningRule(Guid columnId, CultureInfo culture)
    : ColumnCleaningRule(columnId, "normalize-date")
{
    protected override CleaningResult ApplyToCell(CleaningContext context)
    {
        if (context.Cell.CurrentValue is DateTime date)
        {
            return Replace(context, DateTime.SpecifyKind(date, DateTimeKind.Unspecified), "Normalized the date value.");
        }

        return context.Cell.CurrentValue is string value
            && DateTime.TryParse(value, culture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
                ? Replace(context, DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), "Parsed a culture-aware date value.")
                : new CleaningResult(context.Cell, false);
    }
}

public sealed class DecimalCleaningRule(Guid columnId, CultureInfo culture)
    : ColumnCleaningRule(columnId, "normalize-decimal")
{
    protected override CleaningResult ApplyToCell(CleaningContext context)
    {
        if (context.Cell.CurrentValue is decimal)
        {
            return new CleaningResult(context.Cell, false);
        }

        return decimal.TryParse(
            Convert.ToString(context.Cell.CurrentValue, culture),
            NumberStyles.Number,
            culture,
            out var number)
                ? Replace(context, number, "Parsed a culture-aware decimal value.")
                : new CleaningResult(context.Cell, false);
    }
}
