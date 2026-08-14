using System.Text.Json;
using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Profiles;
using DataCleaner.Domain.Validation;
using Microsoft.EntityFrameworkCore;

namespace DataCleaner.Infrastructure.Persistence;

internal sealed class ImportProfileRepository(DataCleanerDbContext dbContext) : IImportProfileRepository
{
    public async Task<IReadOnlyList<ImportProfile>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.ImportProfiles
            .AsNoTracking()
            .Include(profile => profile.ColumnMappings)
            .Include(profile => profile.ValidationRules)
            .OrderBy(profile => profile.Name)
            .ToArrayAsync(cancellationToken);
        return entities.Select(ToDomain).ToArray();
    }

    public async Task<ImportProfile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ImportProfiles
            .AsNoTracking()
            .Include(profile => profile.ColumnMappings)
            .Include(profile => profile.ValidationRules)
            .SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(ImportProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var entity = await dbContext.ImportProfiles
            .Include(candidate => candidate.ColumnMappings)
            .Include(candidate => candidate.ValidationRules)
            .SingleOrDefaultAsync(candidate => candidate.Id == profile.Id, cancellationToken);

        if (entity is null)
        {
            entity = new ImportProfileEntity
            {
                Id = profile.Id,
                Name = profile.Name,
                ProfileVersion = profile.ProfileVersion,
                CreatedAtUtc = profile.CreatedAtUtc,
                UpdatedAtUtc = profile.UpdatedAtUtc
            };
            dbContext.ImportProfiles.Add(entity);
        }

        entity.Name = profile.Name;
        entity.ProfileVersion = profile.ProfileVersion;
        entity.UpdatedAtUtc = profile.UpdatedAtUtc;
        entity.CultureName = profile.CultureName;
        entity.DateFormat = profile.DateFormat;
        entity.NumberFormat = profile.NumberFormat;
        foreach (var existingMapping in entity.ColumnMappings.ToArray())
        {
            if (!profile.ColumnMappings.Any(mapping => string.Equals(
                mapping.SourceColumn,
                existingMapping.SourceColumn,
                StringComparison.OrdinalIgnoreCase)))
            {
                dbContext.ColumnMappings.Remove(existingMapping);
            }
        }

        foreach (var mapping in profile.ColumnMappings)
        {
            var mappingEntity = entity.ColumnMappings.FirstOrDefault(candidate => string.Equals(
                candidate.SourceColumn,
                mapping.SourceColumn,
                StringComparison.OrdinalIgnoreCase));
            if (mappingEntity is null)
            {
                mappingEntity = new ColumnMappingEntity
                {
                    Id = Guid.NewGuid(),
                    ImportProfileId = profile.Id,
                    SourceColumn = mapping.SourceColumn
                };
                entity.ColumnMappings.Add(mappingEntity);
            }

            mappingEntity.SourceColumn = mapping.SourceColumn;
            mappingEntity.TargetField = mapping.TargetField;
            mappingEntity.IsIgnored = mapping.IsIgnored;
        }

        var existingRules = entity.ValidationRules.ToArray();
        for (var index = profile.ValidationRules.Count; index < existingRules.Length; index++)
        {
            dbContext.ValidationRuleConfigurations.Remove(existingRules[index]);
        }

        for (var index = 0; index < profile.ValidationRules.Count; index++)
        {
            var definition = profile.ValidationRules[index];
            var ruleEntity = index < existingRules.Length
                ? existingRules[index]
                : new ValidationRuleConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    ImportProfileId = profile.Id,
                    RuleCode = string.Empty,
                    ConfigurationJson = string.Empty,
                    Severity = string.Empty
                };
            if (index >= existingRules.Length)
            {
                entity.ValidationRules.Add(ruleEntity);
                dbContext.ValidationRuleConfigurations.Add(ruleEntity);
            }

            ruleEntity.RuleCode = definition.Kind.ToString();
            ruleEntity.Severity = definition.Severity.ToString();
            ruleEntity.ConfigurationJson = JsonSerializer.Serialize(new ValidationConfiguration(
                definition.SourceColumn,
                definition.Minimum,
                definition.Maximum,
                definition.AllowedValues.ToArray()));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException(
                "The profile changed while it was being saved. Reload it and try again.",
                exception);
        }
    }

    private static ImportProfile ToDomain(ImportProfileEntity entity) => ImportProfile.Restore(
        entity.Id,
        entity.Name,
        entity.ProfileVersion,
        entity.CreatedAtUtc,
        entity.UpdatedAtUtc,
        entity.CultureName,
        entity.DateFormat,
        entity.NumberFormat,
        entity.ColumnMappings
            .OrderBy(mapping => mapping.Id)
            .Select(mapping => new ColumnMapping(
                mapping.SourceColumn,
                mapping.TargetField,
                mapping.IsIgnored)),
        entity.ValidationRules
            .OrderBy(rule => rule.Id)
            .Select(ToDomain));

    private static ValidationRuleDefinition ToDomain(ValidationRuleConfigurationEntity entity)
    {
        var configuration = JsonSerializer.Deserialize<ValidationConfiguration>(entity.ConfigurationJson)
            ?? throw new InvalidDataException("A persisted validation rule has invalid configuration.");
        if (!Enum.TryParse<ValidationRuleKind>(entity.RuleCode, out var kind)
            || !Enum.TryParse<ValidationSeverity>(entity.Severity, out var severity))
        {
            throw new InvalidDataException("A persisted validation rule has an unknown kind or severity.");
        }

        return new ValidationRuleDefinition(
            configuration.SourceColumn,
            kind,
            severity,
            configuration.Minimum,
            configuration.Maximum,
            configuration.AllowedValues);
    }

    private sealed record ValidationConfiguration(
        string SourceColumn,
        decimal? Minimum,
        decimal? Maximum,
        string[] AllowedValues);
}
