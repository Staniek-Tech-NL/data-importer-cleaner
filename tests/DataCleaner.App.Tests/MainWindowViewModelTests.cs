using System.Globalization;
using DataCleaner.App;
using DataCleaner.Application.Abstractions;
using DataCleaner.Application.Processing;
using DataCleaner.Application.Profiling;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Duplicates;
using DataCleaner.Domain.Profiles;
using DataCleaner.Domain.Validation;

namespace DataCleaner.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task ProfileCultureOverridesWindowsCultureBeforeImportAndFlowsThroughPipeline()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
            var profile = CreateProfile("Polish profile", "pl-PL", "GrossAmount");
            var harness = CreateHarness(
                CreateDataset(),
                [profile],
                request => CreateDataset(
                    value: decimal.Parse(
                        "1 234,50",
                        NumberStyles.Number,
                        CultureInfo.GetCultureInfo(request.CultureName)),
                    sourceValue: "1 234,50"));
            await harness.ViewModel.InitializeAsync();

            harness.ViewModel.SelectedProfile = profile;
            await harness.ViewModel.PrepareFileAsync("customers.csv");
            await harness.ViewModel.RunValidationAsync();
            await harness.ViewModel.RunCleaningAsync();

            Assert.Equal("pl-PL", harness.ImportService.LastRequest?.CultureName);
            Assert.All(harness.ProfilingService.CultureNames, name => Assert.Equal("pl-PL", name));
            Assert.All(harness.ValidationService.CultureNames, name => Assert.Equal("pl-PL", name));
            Assert.All(harness.CleaningService.CultureNames, name => Assert.Equal("pl-PL", name));
            Assert.Equal("pl-PL", harness.ViewModel.EffectiveCultureName);
            Assert.Equal("GrossAmount", Assert.Single(harness.ViewModel.ColumnProfiles).TargetField);
            Assert.Equal(1234.50m, harness.ViewModel.Preview?[0]["GrossAmount"]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public async Task SelectedCultureIsForwardedToImportAndProfiling()
    {
        var harness = CreateHarness(CreateDataset());
        harness.ViewModel.SelectedCultureName = "de-DE";

        await harness.ViewModel.PrepareFileAsync("customers.csv");

        Assert.Equal("de-DE", harness.ImportService.LastRequest?.CultureName);
        Assert.Equal(["de-DE"], harness.ProfilingService.CultureNames);
        Assert.Equal("de-DE", harness.ViewModel.EffectiveCultureName);
    }

    [Fact]
    public async Task SavingProfilePersistsEffectiveDatasetCulture()
    {
        var harness = CreateHarness(CreateDataset());
        harness.ViewModel.SelectedCultureName = "fr-FR";
        await harness.ViewModel.PrepareFileAsync("customers.csv");
        harness.ViewModel.ProfileName = "French data";

        await harness.ViewModel.SaveProfileAsync();

        Assert.Equal("fr-FR", Assert.Single(harness.ProfileRepository.Profiles).CultureName);
    }

    [Fact]
    public async Task CultureChangeAfterImportAppliesOnlyToNextImport()
    {
        var harness = CreateHarness(CreateDataset());
        harness.ViewModel.SelectedCultureName = "en-US";
        await harness.ViewModel.PrepareFileAsync("customers.csv");

        harness.ViewModel.SelectedCultureName = "pl-PL";

        Assert.Equal("en-US", harness.ViewModel.EffectiveCultureName);
        Assert.Contains("next import", harness.ViewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyingDifferentCultureProfileAfterImportRequestsReimport()
    {
        var profile = CreateProfile("Polish profile", "pl-PL", "GrossAmount");
        var harness = CreateHarness(CreateDataset(), [profile]);
        harness.ViewModel.SelectedCultureName = "en-US";
        await harness.ViewModel.PrepareFileAsync("customers.csv");

        harness.ViewModel.SelectedProfile = profile;
        harness.ViewModel.ApplySelectedProfile();

        Assert.Equal("en-US", harness.ViewModel.EffectiveCultureName);
        Assert.Equal("GrossAmount", Assert.Single(harness.ViewModel.ColumnProfiles).TargetField);
        Assert.Contains("Re-import", harness.ViewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewIsLimitedWhileDatasetSummaryKeepsFullRowCount()
    {
        var harness = CreateHarness(CreateDataset(1505));

        await harness.ViewModel.PrepareFileAsync("large.csv");

        Assert.Equal(MainWindowViewModel.PreviewRowLimit, harness.ViewModel.Preview?.Count);
        Assert.Contains("preview", harness.ViewModel.DatasetSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1,505", harness.ViewModel.DatasetSummary?.Replace('\u00A0', ','), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfigurationChangeClearsPreviousValidationState()
    {
        var harness = CreateHarness(CreateDataset());
        await harness.ViewModel.PrepareFileAsync("customers.csv");
        await harness.ViewModel.RunValidationAsync();
        Assert.StartsWith("0 errors", harness.ViewModel.ValidationSummary, StringComparison.Ordinal);

        Assert.Single(harness.ViewModel.ColumnProfiles).IsRequired = true;

        Assert.Contains("not been run", harness.ViewModel.ValidationSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportUpdatesHistoryAndRetainsFullDataset()
    {
        var harness = CreateHarness(CreateDataset(1200));
        await harness.ViewModel.PrepareFileAsync("customers.csv");

        await harness.ViewModel.ExportAsync("clean.csv");

        Assert.Equal(1200, harness.ExportService.LastRequest?.Dataset.Rows.Count);
        Assert.Contains(harness.HistoryRepository.Entries, entry => entry.Status == "Exported");
        Assert.Contains("1,200", harness.ViewModel.StatusMessage.Replace('\u00A0', ','), StringComparison.Ordinal);
    }

    private static Harness CreateHarness(
        ImportedDataset dataset,
        IReadOnlyList<ImportProfile>? profiles = null,
        Func<ImportRequest, ImportedDataset>? datasetFactory = null)
    {
        var importService = new RecordingImportService(datasetFactory ?? (_ => dataset));
        var profilingService = new RecordingProfilingService();
        var validationService = new RecordingValidationService();
        var cleaningService = new RecordingCleaningService();
        var exportService = new RecordingExportService();
        var profileRepository = new RecordingProfileRepository(profiles ?? []);
        var historyRepository = new RecordingHistoryRepository();
        var viewModel = new MainWindowViewModel(
            importService,
            profilingService,
            validationService,
            cleaningService,
            new EmptyDuplicateService(),
            exportService,
            new EmptyErrorReportWriter(),
            profileRepository,
            historyRepository);
        return new Harness(
            viewModel,
            importService,
            profilingService,
            validationService,
            cleaningService,
            exportService,
            profileRepository,
            historyRepository);
    }

    private static ImportedDataset CreateDataset(
        int rowCount = 1,
        decimal value = 1.25m,
        string sourceValue = "1,25")
    {
        var column = new ImportedColumn(Guid.NewGuid(), 0, "Amount", DataType.Decimal);
        var rows = Enumerable.Range(1, rowCount)
            .Select(index => new ImportedRow(index, [new DataCell(column.Id, sourceValue, value)]));
        return new ImportedDataset("customers.csv", [column], rows);
    }

    private static ImportProfile CreateProfile(string name, string cultureName, string targetField) =>
        new(name, cultureName, [new ColumnMapping("Amount", targetField)]);

    private sealed record Harness(
        MainWindowViewModel ViewModel,
        RecordingImportService ImportService,
        RecordingProfilingService ProfilingService,
        RecordingValidationService ValidationService,
        RecordingCleaningService CleaningService,
        RecordingExportService ExportService,
        RecordingProfileRepository ProfileRepository,
        RecordingHistoryRepository HistoryRepository);

    private sealed class RecordingImportService(Func<ImportRequest, ImportedDataset> datasetFactory) : IDataImportService
    {
        public ImportRequest? LastRequest { get; private set; }

        public Task<IReadOnlyList<string>> GetWorksheetNamesAsync(
            string filePath,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(["Sheet1"]);

        public Task<ImportedDataset> ImportAsync(
            ImportRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(datasetFactory(request));
        }
    }

    private sealed class RecordingProfilingService : IDataProfilingService
    {
        private readonly DataProfilingService _inner = new();

        public List<string> CultureNames { get; } = [];

        public IReadOnlyList<DataCleaner.Domain.Profiling.ColumnProfile> Profile(
            ImportedDataset dataset,
            string cultureName)
        {
            CultureNames.Add(cultureName);
            return _inner.Profile(dataset, cultureName);
        }
    }

    private sealed class RecordingValidationService : IDataValidationService
    {
        public List<string> CultureNames { get; } = [];

        public Task<ValidationResult> ValidateAsync(
            ImportedDataset dataset,
            IEnumerable<ValidationRuleDefinition> definitions,
            ValidationPass pass,
            string cultureName,
            CancellationToken cancellationToken = default)
        {
            CultureNames.Add(cultureName);
            return Task.FromResult(new ValidationResult(pass, DateTimeOffset.UtcNow, [], []));
        }
    }

    private sealed class RecordingCleaningService : IDataCleaningService
    {
        public List<string> CultureNames { get; } = [];

        public Task<CleaningRunResult> CleanAsync(
            ImportedDataset dataset,
            IEnumerable<CleaningRuleDefinition> definitions,
            string cultureName,
            CancellationToken cancellationToken = default)
        {
            CultureNames.Add(cultureName);
            return Task.FromResult(new CleaningRunResult(dataset, DateTimeOffset.UtcNow, []));
        }
    }

    private sealed class EmptyDuplicateService : IDuplicateDetectionService
    {
        public Task<DuplicateDetectionResult> DetectAsync(
            ImportedDataset dataset,
            DuplicateDefinition definition,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DuplicateDetectionResult(DateTimeOffset.UtcNow, []));

        public Task<DuplicateResolutionResult> ResolveAsync(
            ImportedDataset dataset,
            DuplicateDefinition definition,
            DuplicateResolutionAction action,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DuplicateResolutionResult(
                dataset,
                new DuplicateDetectionResult(DateTimeOffset.UtcNow, []),
                action,
                []));
    }

    private sealed class RecordingExportService : IDataExportService
    {
        public ExportRequest? LastRequest { get; private set; }

        public Task<ExportResult> ExportAsync(
            ExportRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new ExportResult(
                request.FilePath,
                request.Dataset.Rows.Count,
                request.Filter,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class EmptyErrorReportWriter : IErrorReportWriter
    {
        public Task WriteAsync(
            string filePath,
            IEnumerable<ErrorReportRow> rows,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingProfileRepository(IReadOnlyList<ImportProfile> profiles) : IImportProfileRepository
    {
        public List<ImportProfile> Profiles { get; } = [.. profiles];

        public Task<IReadOnlyList<ImportProfile>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ImportProfile>>(Profiles.ToArray());

        public Task<ImportProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Profiles.FirstOrDefault(profile => profile.Id == id));

        public Task SaveAsync(ImportProfile profile, CancellationToken cancellationToken = default)
        {
            var index = Profiles.FindIndex(candidate => candidate.Id == profile.Id);
            if (index >= 0)
            {
                Profiles[index] = profile;
            }
            else
            {
                Profiles.Add(profile);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHistoryRepository : IImportHistoryRepository
    {
        public List<ImportHistoryEntry> Entries { get; } = [];

        public Task<IReadOnlyList<ImportHistoryEntry>> GetRecentAsync(
            int count,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ImportHistoryEntry>>(Entries.TakeLast(count).Reverse().ToArray());

        public Task SaveAsync(ImportHistoryEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }
}
