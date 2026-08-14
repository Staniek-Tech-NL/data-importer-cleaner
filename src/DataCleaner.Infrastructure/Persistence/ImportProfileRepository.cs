using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Profiles;
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
            .SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task SaveAsync(ImportProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var entity = await dbContext.ImportProfiles
            .Include(candidate => candidate.ColumnMappings)
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

        await dbContext.SaveChangesAsync(cancellationToken);
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
                mapping.IsIgnored)));
}
