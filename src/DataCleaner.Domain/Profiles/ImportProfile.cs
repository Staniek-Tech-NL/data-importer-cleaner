using DataCleaner.Domain.Validation;

namespace DataCleaner.Domain.Profiles;

public sealed class ImportProfile
{
    private IReadOnlyList<ColumnMapping> _columnMappings;
    private IReadOnlyList<ValidationRuleDefinition> _validationRules;

    public ImportProfile(
        string name,
        string? cultureName = null,
        IEnumerable<ColumnMapping>? columnMappings = null,
        IEnumerable<ValidationRuleDefinition>? validationRules = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        Name = name.Trim();
        CultureName = NormalizeOptional(cultureName);
        _columnMappings = ValidateMappings(columnMappings ?? []);
        _validationRules = ValidateRules(validationRules ?? []);
        ProfileVersion = 1;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    private ImportProfile(
        Guid id,
        string name,
        int profileVersion,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? cultureName,
        string? dateFormat,
        string? numberFormat,
        IEnumerable<ColumnMapping> columnMappings,
        IEnumerable<ValidationRuleDefinition> validationRules)
    {
        Id = id;
        Name = name;
        ProfileVersion = profileVersion;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        CultureName = cultureName;
        DateFormat = dateFormat;
        NumberFormat = numberFormat;
        _columnMappings = ValidateMappings(columnMappings);
        _validationRules = ValidateRules(validationRules);
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public int ProfileVersion { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public string? CultureName { get; private set; }

    public string? DateFormat { get; private set; }

    public string? NumberFormat { get; private set; }

    public IReadOnlyList<ColumnMapping> ColumnMappings => _columnMappings;

    public IReadOnlyList<ValidationRuleDefinition> ValidationRules => _validationRules;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedName = name.Trim();
        if (string.Equals(Name, normalizedName, StringComparison.Ordinal))
        {
            return;
        }

        Name = normalizedName;
        MarkUpdated();
    }

    public void UpdateConfiguration(
        string? cultureName,
        IEnumerable<ColumnMapping> columnMappings,
        string? dateFormat = null,
        string? numberFormat = null,
        IEnumerable<ValidationRuleDefinition>? validationRules = null)
    {
        var normalizedCulture = NormalizeOptional(cultureName);
        var normalizedDateFormat = NormalizeOptional(dateFormat);
        var normalizedNumberFormat = NormalizeOptional(numberFormat);
        var mappings = ValidateMappings(columnMappings);
        var rules = validationRules is null ? _validationRules : ValidateRules(validationRules);

        if (string.Equals(CultureName, normalizedCulture, StringComparison.Ordinal)
            && string.Equals(DateFormat, normalizedDateFormat, StringComparison.Ordinal)
            && string.Equals(NumberFormat, normalizedNumberFormat, StringComparison.Ordinal)
            && _columnMappings.SequenceEqual(mappings)
            && ValidationRulesEqual(_validationRules, rules))
        {
            return;
        }

        CultureName = normalizedCulture;
        DateFormat = normalizedDateFormat;
        NumberFormat = normalizedNumberFormat;
        _columnMappings = mappings;
        _validationRules = rules;
        MarkUpdated();
    }

    public static ImportProfile Restore(
        Guid id,
        string name,
        int profileVersion,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? cultureName,
        string? dateFormat,
        string? numberFormat,
        IEnumerable<ColumnMapping> columnMappings,
        IEnumerable<ValidationRuleDefinition>? validationRules = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A persisted profile must have an identifier.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(profileVersion);
        return new ImportProfile(
            id,
            name,
            profileVersion,
            createdAtUtc,
            updatedAtUtc,
            NormalizeOptional(cultureName),
            NormalizeOptional(dateFormat),
            NormalizeOptional(numberFormat),
            columnMappings,
            validationRules ?? []);
    }

    private static ColumnMapping[] ValidateMappings(IEnumerable<ColumnMapping> mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        var result = mappings.ToArray();
        if (result.Select(mapping => mapping.SourceColumn).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != result.Length)
        {
            throw new ArgumentException("Source columns must be unique within an import profile.", nameof(mappings));
        }

        return result;
    }

    private static ValidationRuleDefinition[] ValidateRules(IEnumerable<ValidationRuleDefinition> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var result = rules.ToArray();
        var uniqueKeys = result
            .Select(rule => $"{rule.SourceColumn.ToUpperInvariant()}|{rule.Kind}")
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (uniqueKeys != result.Length)
        {
            throw new ArgumentException(
                "A validation rule kind can be configured only once per source column.",
                nameof(rules));
        }

        return result;
    }

    private static bool ValidationRulesEqual(
        IReadOnlyList<ValidationRuleDefinition> first,
        IReadOnlyList<ValidationRuleDefinition> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        return first.All(rule => second.Any(candidate =>
            string.Equals(rule.SourceColumn, candidate.SourceColumn, StringComparison.Ordinal)
            && rule.Kind == candidate.Kind
            && rule.Severity == candidate.Severity
            && rule.Minimum == candidate.Minimum
            && rule.Maximum == candidate.Maximum
            && rule.AllowedValues.SequenceEqual(candidate.AllowedValues, StringComparer.Ordinal)));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void MarkUpdated()
    {
        ProfileVersion++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
