using DataCleaner.Domain.Duplicates;
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
}
