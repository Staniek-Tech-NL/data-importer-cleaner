namespace DataCleaner.Infrastructure.Persistence;

internal static class DatabasePath
{
    public static string GetDefault()
    {
        var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(applicationData, "DataCleaner");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "datacleaner.db");
    }
}
