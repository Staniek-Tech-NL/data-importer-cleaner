using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Profiles;
using DataCleaner.Infrastructure;
using DataCleaner.Infrastructure.Persistence;
using DataCleaner.Domain.Validation;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace DataCleaner.Infrastructure.Tests;

public sealed class ImportProfileRepositoryTests
{
    [Fact]
    public async Task Repository_PersistsAndUpdatesVersionedColumnMappings()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DataCleaner.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "profiles.db");
        Directory.CreateDirectory(directory);

        try
        {
            var services = new ServiceCollection();
            services.AddInfrastructure(databasePath);
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
            var repository = scope.ServiceProvider.GetRequiredService<IImportProfileRepository>();
            var profile = new ImportProfile(
                "Customer CSV",
                "pl-PL",
                [
                    new ColumnMapping("Email", "EmailAddress"),
                    new ColumnMapping("Notes", null, isIgnored: true)
                ]);

            await repository.SaveAsync(profile);
            profile.UpdateConfiguration(
                "pl-PL",
                [new ColumnMapping("Email", "PrimaryEmail", isDuplicateKey: true)],
                validationRules:
                [
                    new ValidationRuleDefinition(
                        "Email",
                        ValidationRuleKind.Required,
                        ValidationSeverity.Error),
                    new ValidationRuleDefinition(
                        "Email",
                        ValidationRuleKind.Email,
                        ValidationSeverity.Warning)
                ],
                cleaningRules:
                [
                    new CleaningRuleDefinition("Email", CleaningRuleKind.Trim, 0),
                    new CleaningRuleDefinition("Email", CleaningRuleKind.NormalizeEmail, 1),
                    new CleaningRuleDefinition(
                        "Email",
                        CleaningRuleKind.NullTokens,
                        2,
                        ["N/A", "NULL"]),
                    new CleaningRuleDefinition(
                        "Email",
                        CleaningRuleKind.CountryAlias,
                        3,
                        aliases: new Dictionary<string, string> { ["NL"] = "Netherlands" })
                ]);
            await repository.SaveAsync(profile);

            var restored = await repository.GetByIdAsync(profile.Id);
            var all = await repository.GetAllAsync();

            Assert.NotNull(restored);
            Assert.Equal(2, restored.ProfileVersion);
            Assert.Equal("pl-PL", restored.CultureName);
            Assert.Equal("PrimaryEmail", Assert.Single(restored.ColumnMappings).TargetField);
            Assert.True(Assert.Single(restored.ColumnMappings).IsDuplicateKey);
            Assert.Equal(2, restored.ValidationRules.Count);
            Assert.Contains(restored.ValidationRules, rule => rule.Kind == ValidationRuleKind.Email);
            Assert.Equal(4, restored.CleaningRules.Count);
            Assert.Equal(CleaningRuleKind.Trim, restored.CleaningRules[0].Kind);
            Assert.Contains(restored.CleaningRules, rule =>
                rule.Kind == CleaningRuleKind.NullTokens && rule.Values.Contains("NULL"));
            Assert.Contains(restored.CleaningRules, rule =>
                rule.Kind == CleaningRuleKind.CountryAlias
                && rule.Aliases.GetValueOrDefault("NL") == "Netherlands");
            Assert.Single(all);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }
}
