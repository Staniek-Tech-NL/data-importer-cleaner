namespace DataCleaner.Infrastructure.Persistence;

internal sealed class ImportProfileEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int ProfileVersion { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? CultureName { get; set; }
    public string? DateFormat { get; set; }
    public string? NumberFormat { get; set; }
    public List<ColumnMappingEntity> ColumnMappings { get; set; } = [];
    public List<ValidationRuleConfigurationEntity> ValidationRules { get; set; } = [];
    public List<CleaningRuleConfigurationEntity> CleaningRules { get; set; } = [];
}

internal sealed class ColumnMappingEntity
{
    public Guid Id { get; set; }
    public Guid ImportProfileId { get; set; }
    public required string SourceColumn { get; set; }
    public string? TargetField { get; set; }
    public bool IsIgnored { get; set; }
    public bool IsDuplicateKey { get; set; }
}

internal sealed class ValidationRuleConfigurationEntity
{
    public Guid Id { get; set; }
    public Guid ImportProfileId { get; set; }
    public required string RuleCode { get; set; }
    public required string ConfigurationJson { get; set; }
    public required string Severity { get; set; }
}

internal sealed class CleaningRuleConfigurationEntity
{
    public Guid Id { get; set; }
    public Guid ImportProfileId { get; set; }
    public required string RuleCode { get; set; }
    public required string ConfigurationJson { get; set; }
    public int ExecutionOrder { get; set; }
}

internal sealed class ImportJobEntity
{
    public Guid Id { get; set; }
    public required string SourceFileName { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public required string Status { get; set; }
    public int ProcessedRows { get; set; }
    public int InvalidRows { get; set; }
    public ImportResultEntity? Result { get; set; }
}

internal sealed class ImportResultEntity
{
    public Guid Id { get; set; }
    public Guid ImportJobId { get; set; }
    public int ValidRows { get; set; }
    public int ModifiedRows { get; set; }
    public int DuplicatesRemoved { get; set; }
    public string? OutputFileName { get; set; }
}
