using System.Diagnostics;
using System.Globalization;
using System.Text;
using DataCleaner.Application.Abstractions;
using DataCleaner.Application.Processing;
using DataCleaner.Application.Profiling;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Duplicates;
using DataCleaner.Domain.Validation;
using DataCleaner.Infrastructure.Files;
using Xunit.Abstractions;

namespace DataCleaner.Infrastructure.Tests;

public sealed class PerformanceSmokeTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Performance")]
    public async Task FiftyThousandRows_CompleteCorePipelineWithinPracticalBudget()
    {
        const int rowCount = 50_000;
        var inputPath = Path.Combine(Path.GetTempPath(), $"DataCleaner-perf-{Guid.NewGuid():N}.csv");
        var outputPath = Path.Combine(Path.GetTempPath(), $"DataCleaner-perf-{Guid.NewGuid():N}-output.csv");
        try
        {
            await WriteSyntheticCsvAsync(inputPath, rowCount);
            var stopwatch = Stopwatch.StartNew();
            var dataset = await new CsvDataFileReader().ReadAsync(new ImportRequest(
                inputPath, null, "en-US", "UTF-8"));
            var importedAt = stopwatch.Elapsed;
            var profiles = new DataProfilingService().Profile(dataset, "en-US");
            var profiledAt = stopwatch.Elapsed;
            var cleaned = await new DataCleaningService(new CleaningEngine()).CleanAsync(
                dataset,
                [
                    new CleaningRuleDefinition("Full Name", CleaningRuleKind.Trim, 0),
                    new CleaningRuleDefinition("Email", CleaningRuleKind.Trim, 1),
                    new CleaningRuleDefinition("Email", CleaningRuleKind.NormalizeEmail, 2)
                ],
                "en-US");
            var cleanedAt = stopwatch.Elapsed;
            var validation = await new DataValidationService(new ValidationEngine()).ValidateAsync(
                cleaned.Dataset,
                [new ValidationRuleDefinition("Email", ValidationRuleKind.Email, ValidationSeverity.Error)],
                ValidationPass.AfterCleaning,
                "en-US");
            var emailColumn = cleaned.Dataset.Columns.Single(column => column.SourceName == "Email");
            var countryColumn = cleaned.Dataset.Columns.Single(column => column.SourceName == "Country");
            var duplicates = await new DuplicateDetectionService().DetectAsync(
                cleaned.Dataset,
                new DuplicateDefinition([emailColumn.Id, countryColumn.Id]));
            await new CsvDataFileWriter().WriteAsync(new ExportRequest(outputPath, cleaned.Dataset));
            stopwatch.Stop();

            output.WriteLine(
                $"50k pipeline: import={importedAt.TotalMilliseconds:F0}ms, profile={(profiledAt - importedAt).TotalMilliseconds:F0}ms, clean={(cleanedAt - profiledAt).TotalMilliseconds:F0}ms, total={stopwatch.Elapsed.TotalMilliseconds:F0}ms");
            Assert.Equal(rowCount, dataset.Rows.Count);
            Assert.Equal(7, profiles.Count);
            Assert.Empty(validation.Issues);
            Assert.NotEmpty(duplicates.Groups);
            Assert.True(File.Exists(outputPath));
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30),
                $"The 50,000-row core pipeline took {stopwatch.Elapsed.TotalSeconds:F1}s.");
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    private static async Task WriteSyntheticCsvAsync(string path, int rows)
    {
        await using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        await writer.WriteLineAsync("Customer ID,Full Name,Email,Country,Signup Date,Revenue,Notes");
        for (var index = 1; index <= rows; index++)
        {
            var duplicateSeed = index % 49_000;
            await writer.WriteLineAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"C-{index:D7},  Synthetic User {index}  , customer{duplicateSeed}@EXAMPLE.COM ,PL,2026-01-01,{index % 1000}.00,generated"));
        }
    }
}
