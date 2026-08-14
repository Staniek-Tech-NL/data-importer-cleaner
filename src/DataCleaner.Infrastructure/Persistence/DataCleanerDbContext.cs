using Microsoft.EntityFrameworkCore;

namespace DataCleaner.Infrastructure.Persistence;

public sealed class DataCleanerDbContext(DbContextOptions<DataCleanerDbContext> options)
    : DbContext(options)
{
    internal DbSet<ImportProfileEntity> ImportProfiles => Set<ImportProfileEntity>();
    internal DbSet<ColumnMappingEntity> ColumnMappings => Set<ColumnMappingEntity>();
    internal DbSet<ValidationRuleConfigurationEntity> ValidationRuleConfigurations => Set<ValidationRuleConfigurationEntity>();
    internal DbSet<CleaningRuleConfigurationEntity> CleaningRuleConfigurations => Set<CleaningRuleConfigurationEntity>();
    internal DbSet<ImportJobEntity> ImportJobs => Set<ImportJobEntity>();
    internal DbSet<ImportResultEntity> ImportResults => Set<ImportResultEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportProfileEntity>(entity =>
        {
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Name).HasMaxLength(200);
            entity.HasIndex(profile => profile.Name).IsUnique();
            entity.HasMany(profile => profile.ColumnMappings)
                .WithOne()
                .HasForeignKey(mapping => mapping.ImportProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(profile => profile.ValidationRules)
                .WithOne()
                .HasForeignKey(rule => rule.ImportProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(profile => profile.CleaningRules)
                .WithOne()
                .HasForeignKey(rule => rule.ImportProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ColumnMappingEntity>(entity =>
        {
            entity.HasKey(mapping => mapping.Id);
            entity.Property(mapping => mapping.SourceColumn).HasMaxLength(300);
            entity.Property(mapping => mapping.TargetField).HasMaxLength(300);
        });

        modelBuilder.Entity<ValidationRuleConfigurationEntity>(entity =>
        {
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.RuleCode).HasMaxLength(100);
            entity.Property(rule => rule.Severity).HasMaxLength(20);
        });

        modelBuilder.Entity<CleaningRuleConfigurationEntity>(entity =>
        {
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.RuleCode).HasMaxLength(100);
        });

        modelBuilder.Entity<ImportJobEntity>(entity =>
        {
            entity.HasKey(job => job.Id);
            entity.Property(job => job.SourceFileName).HasMaxLength(260);
            entity.Property(job => job.Status).HasMaxLength(50);
            entity.HasOne(job => job.Result)
                .WithOne()
                .HasForeignKey<ImportResultEntity>(result => result.ImportJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportResultEntity>(entity =>
        {
            entity.HasKey(result => result.Id);
            entity.Property(result => result.OutputFileName).HasMaxLength(260);
        });
    }
}
