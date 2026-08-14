using DataCleaner.Domain.Duplicates;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Profiles;

namespace DataCleaner.Domain.Tests;

public sealed class ProfileAndDuplicateDefinitionTests
{
    [Fact]
    public void DuplicateDefinition_DefaultsToNormalizedCurrentValues()
    {
        var definition = new DuplicateDefinition([Guid.NewGuid()]);

        Assert.Equal(DuplicateComparisonSource.CurrentNormalizedValue, definition.ComparisonSource);
    }

    [Fact]
    public void RenamingProfile_IncrementsVersion()
    {
        var profile = new ImportProfile("Customer Import NL");

        profile.Rename("Customer Import Europe");

        Assert.Equal(2, profile.ProfileVersion);
        Assert.Equal("Customer Import Europe", profile.Name);
        Assert.True(profile.UpdatedAtUtc >= profile.CreatedAtUtc);
    }

    [Fact]
    public void UpdatingProfileMappings_IncrementsVersionOnlyForARealChange()
    {
        var profile = new ImportProfile(
            "Customer Import",
            "en-US",
            [new ColumnMapping("Email", "EmailAddress")]);

        profile.UpdateConfiguration(
            "en-US",
            [new ColumnMapping("Email", "EmailAddress")]);
        Assert.Equal(1, profile.ProfileVersion);

        profile.UpdateConfiguration(
            "en-US",
            [new ColumnMapping("Email", "PrimaryEmail")]);

        Assert.Equal(2, profile.ProfileVersion);
        Assert.Equal("PrimaryEmail", profile.ColumnMappings[0].TargetField);
    }

    [Fact]
    public void Profile_RejectsDuplicateSourceMappings()
    {
        var action = () => new ImportProfile(
            "Invalid",
            columnMappings:
            [
                new ColumnMapping("Email", "PrimaryEmail"),
                new ColumnMapping("email", "BackupEmail")
            ]);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void UpdatingCleaningRules_VersionsOnlyRealConfigurationChanges()
    {
        var profile = new ImportProfile(
            "Cleaning profile",
            cleaningRules: [new CleaningRuleDefinition("Name", CleaningRuleKind.Trim, 0)]);

        profile.UpdateConfiguration(
            null,
            [],
            cleaningRules: [new CleaningRuleDefinition("Name", CleaningRuleKind.Trim, 0)]);
        Assert.Equal(1, profile.ProfileVersion);

        profile.UpdateConfiguration(
            null,
            [],
            cleaningRules:
            [
                new CleaningRuleDefinition("Name", CleaningRuleKind.Trim, 0),
                new CleaningRuleDefinition("Name", CleaningRuleKind.TitleCase, 1)
            ]);

        Assert.Equal(2, profile.ProfileVersion);
    }
}
