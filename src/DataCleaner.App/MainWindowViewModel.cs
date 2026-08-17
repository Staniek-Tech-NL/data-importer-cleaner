using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Runtime.CompilerServices;
using DataCleaner.Application.Abstractions;
using DataCleaner.Application.Profiling;
using DataCleaner.Application.Processing;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Profiles;
using DataCleaner.Domain.Validation;
using DataCleaner.Domain.Duplicates;

namespace DataCleaner.App;

public sealed class MainWindowViewModel(
    IDataImportService importService,
    IDataProfilingService profilingService,
    IDataValidationService validationService,
    IDataCleaningService cleaningService,
    IDuplicateDetectionService duplicateDetectionService,
    IDataExportService exportService,
    IErrorReportWriter errorReportWriter,
    IImportProfileRepository profileRepository,
    IImportHistoryRepository historyRepository) : INotifyPropertyChanged
{
    public const int PreviewRowLimit = 1000;

    private string _statusMessage = "Select a CSV file to inspect its contents safely.";
    private DataView? _preview;
    private string? _datasetSummary;
    private bool _isImportEnabled = true;
    private IReadOnlyList<string> _worksheetNames = [];
    private string? _selectedWorksheet;
    private string _selectedEncoding = "UTF-8";
    private string _selectedCultureName = GetDefaultCultureName();
    private string? _datasetCultureName;
    private string? _pendingFilePath;
    private ImportedDataset? _dataset;
    private IReadOnlyList<ImportProfile> _savedProfiles = [];
    private ImportProfile? _selectedProfile;
    private string _profileName = string.Empty;
    private IReadOnlyList<ValidationIssueViewModel> _validationIssues = [];
    private IReadOnlyList<RejectedRowViewModel> _rejectedRows = [];
    private string _validationSummary = "Validation has not been run.";
    private IReadOnlyList<CleaningChangeViewModel> _cleaningChanges = [];
    private string _cleaningSummary = "Cleaning has not been run.";
    private IReadOnlyList<DuplicateGroupViewModel> _duplicateGroups = [];
    private string _duplicateSummary = "Duplicate detection has not been run.";
    private DuplicateResolutionAction _selectedDuplicateAction = DuplicateResolutionAction.MarkForReview;
    private ExportRowFilter _selectedExportFilter;
    private IReadOnlyList<ImportHistoryEntry> _historyEntries = [];
    private string _processingSummary = "No active import.";
    private Guid _currentImportId;
    private DateTimeOffset _importStartedAtUtc;
    private int _sourceRowCount;
    private int _duplicatesRemoved;
    private bool _suppressConfigurationRefresh;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ApplicationName => "Data Importer & Cleaner · 1.0.0";

    public IReadOnlyList<string> AvailableEncodings { get; } = ["UTF-8", "Windows-1250"];

    public IReadOnlyList<string> AvailableCultureNames { get; } = BuildAvailableCultureNames();

    public string SelectedEncoding
    {
        get => _selectedEncoding;
        set => SetField(ref _selectedEncoding, value);
    }

    public string SelectedCultureName
    {
        get => _selectedCultureName;
        set
        {
            var normalizedValue = NormalizeCultureName(value);
            if (!SetField(ref _selectedCultureName, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(EffectiveCultureName));
            if (_datasetCultureName is not null
                && !string.Equals(_datasetCultureName, normalizedValue, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Data culture {normalizedValue} will apply on the next import. Re-import the source to use it for parsing.";
            }
        }
    }

    public string EffectiveCultureName => _datasetCultureName ?? SelectedCultureName;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public DataView? Preview
    {
        get => _preview;
        private set
        {
            if (SetField(ref _preview, value))
            {
                OnPropertyChanged(nameof(HasPreview));
            }
        }
    }

    public string? DatasetSummary
    {
        get => _datasetSummary;
        private set => SetField(ref _datasetSummary, value);
    }

    public bool IsImportEnabled
    {
        get => _isImportEnabled;
        private set => SetField(ref _isImportEnabled, value);
    }

    public bool HasPreview => Preview is not null;

    public ObservableCollection<ColumnProfileViewModel> ColumnProfiles { get; } = [];

    public bool HasColumnProfiles => ColumnProfiles.Count > 0;

    public IReadOnlyList<ValidationIssueViewModel> ValidationIssues
    {
        get => _validationIssues;
        private set => SetField(ref _validationIssues, value);
    }

    public IReadOnlyList<RejectedRowViewModel> RejectedRows
    {
        get => _rejectedRows;
        private set => SetField(ref _rejectedRows, value);
    }

    public string ValidationSummary
    {
        get => _validationSummary;
        private set => SetField(ref _validationSummary, value);
    }

    public IReadOnlyList<CleaningChangeViewModel> CleaningChanges
    {
        get => _cleaningChanges;
        private set => SetField(ref _cleaningChanges, value);
    }

    public string CleaningSummary
    {
        get => _cleaningSummary;
        private set => SetField(ref _cleaningSummary, value);
    }

    public IReadOnlyList<DuplicateResolutionAction> AvailableDuplicateActions { get; } =
        Enum.GetValues<DuplicateResolutionAction>();

    public DuplicateResolutionAction SelectedDuplicateAction
    {
        get => _selectedDuplicateAction;
        set => SetField(ref _selectedDuplicateAction, value);
    }

    public IReadOnlyList<DuplicateGroupViewModel> DuplicateGroups
    {
        get => _duplicateGroups;
        private set => SetField(ref _duplicateGroups, value);
    }

    public string DuplicateSummary
    {
        get => _duplicateSummary;
        private set => SetField(ref _duplicateSummary, value);
    }

    public IReadOnlyList<ExportRowFilter> AvailableExportFilters { get; } =
        Enum.GetValues<ExportRowFilter>();

    public ExportRowFilter SelectedExportFilter
    {
        get => _selectedExportFilter;
        set => SetField(ref _selectedExportFilter, value);
    }

    public IReadOnlyList<ImportHistoryEntry> HistoryEntries
    {
        get => _historyEntries;
        private set => SetField(ref _historyEntries, value);
    }

    public string ProcessingSummary
    {
        get => _processingSummary;
        private set => SetField(ref _processingSummary, value);
    }

    public IReadOnlyList<ImportProfile> SavedProfiles
    {
        get => _savedProfiles;
        private set => SetField(ref _savedProfiles, value);
    }

    public ImportProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (SetField(ref _selectedProfile, value) && value is not null)
            {
                ProfileName = value.Name;
                if (!string.IsNullOrWhiteSpace(value.CultureName))
                {
                    SelectedCultureName = value.CultureName;
                }
            }
        }
    }

    public string ProfileName
    {
        get => _profileName;
        set => SetField(ref _profileName, value ?? string.Empty);
    }

    public IReadOnlyList<string> WorksheetNames
    {
        get => _worksheetNames;
        private set
        {
            if (SetField(ref _worksheetNames, value))
            {
                OnPropertyChanged(nameof(HasWorksheetSelection));
            }
        }
    }

    public string? SelectedWorksheet
    {
        get => _selectedWorksheet;
        set => SetField(ref _selectedWorksheet, value);
    }

    public bool HasWorksheetSelection => WorksheetNames.Count > 1;

    public async Task InitializeAsync()
    {
        try
        {
            await ReloadProfilesAsync();
            await ReloadHistoryAsync();
        }
        catch (DbException exception)
        {
            StatusMessage = $"Saved profiles could not be loaded: {exception.Message}";
        }
    }

    public async Task PrepareFileAsync(string filePath)
    {
        if (!IsImportEnabled)
        {
            return;
        }

        IsImportEnabled = false;
        StatusMessage = $"Opening {Path.GetFileName(filePath)}…";

        try
        {
            WorksheetNames = [];
            SelectedWorksheet = null;
            _pendingFilePath = null;

            if (string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                var worksheetNames = await importService.GetWorksheetNamesAsync(filePath);
                if (worksheetNames.Count > 1)
                {
                    _pendingFilePath = filePath;
                    WorksheetNames = worksheetNames;
                    SelectedWorksheet = worksheetNames[0];
                    StatusMessage = "Select a worksheet to import.";
                    return;
                }

                var worksheetName = worksheetNames.SingleOrDefault();
                await ImportCoreAsync(filePath, worksheetName);
                return;
            }

            await ImportCoreAsync(filePath, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsImportEnabled = true;
        }
    }

    public async Task ImportSelectedWorksheetAsync()
    {
        if (!IsImportEnabled || _pendingFilePath is null || SelectedWorksheet is null)
        {
            return;
        }

        IsImportEnabled = false;
        StatusMessage = $"Importing worksheet {SelectedWorksheet}…";
        try
        {
            await ImportCoreAsync(_pendingFilePath, SelectedWorksheet);
            WorksheetNames = [];
            SelectedWorksheet = null;
            _pendingFilePath = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsImportEnabled = true;
        }
    }

    public async Task SaveProfileAsync()
    {
        if (_dataset is null || ColumnProfiles.Count == 0)
        {
            StatusMessage = "Import a file before saving a profile.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            StatusMessage = "Enter a profile name.";
            return;
        }

        var mappings = ColumnProfiles
            .Select(column => new ColumnMapping(
                column.SourceColumn,
                column.TargetField,
                column.IsIgnored,
                column.IsDuplicateKey))
            .ToArray();
        ValidationRuleDefinition[] validationRules;
        CleaningRuleDefinition[] cleaningRules;
        try
        {
            validationRules = BuildValidationDefinitions();
            cleaningRules = BuildCleaningDefinitions();
        }
        catch (ArgumentException exception)
        {
            StatusMessage = exception.Message;
            return;
        }

        var profile = SelectedProfile is not null
            && string.Equals(SelectedProfile.Name, ProfileName.Trim(), StringComparison.OrdinalIgnoreCase)
                ? SelectedProfile
                : SavedProfiles.FirstOrDefault(candidate => string.Equals(
                    candidate.Name,
                    ProfileName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                    ?? new ImportProfile(
                        ProfileName,
                        EffectiveCultureName,
                        mappings,
                        validationRules,
                        cleaningRules);

        if (profile.ColumnMappings.Count > 0)
        {
            profile.Rename(ProfileName);
            profile.UpdateConfiguration(
                EffectiveCultureName,
                mappings,
                validationRules: validationRules,
                cleaningRules: cleaningRules);
        }

        try
        {
            await profileRepository.SaveAsync(profile);
            await ReloadProfilesAsync(profile.Id);
            StatusMessage = $"Profile '{profile.Name}' version {profile.ProfileVersion} saved.";
        }
        catch (Exception exception) when (exception is DbException or InvalidOperationException or ArgumentException)
        {
            StatusMessage = $"Profile could not be saved: {exception.Message}";
        }
    }

    public void ApplySelectedProfile()
    {
        if (_dataset is null || SelectedProfile is null)
        {
            StatusMessage = "Select a saved profile to apply.";
            return;
        }

        var profile = SelectedProfile;
        var requiresReimport = !string.IsNullOrWhiteSpace(profile.CultureName)
            && !string.Equals(profile.CultureName, EffectiveCultureName, StringComparison.OrdinalIgnoreCase);
        ApplyProfileConfiguration(profile);
        StatusMessage = requiresReimport
            ? $"Profile '{profile.Name}' applied. Re-import the source to parse it with profile culture {profile.CultureName}."
            : $"Profile '{profile.Name}' version {profile.ProfileVersion} applied.";
    }

    private void ApplyProfileConfiguration(ImportProfile profile)
    {
        _suppressConfigurationRefresh = true;
        try
        {
            var mappings = profile.ColumnMappings.ToDictionary(
            mapping => mapping.SourceColumn,
            StringComparer.OrdinalIgnoreCase);
            foreach (var column in ColumnProfiles)
            {
                column.ResetValidationConfiguration();
                column.ResetCleaningConfiguration();
                if (mappings.TryGetValue(column.SourceColumn, out var mapping))
                {
                    column.TargetField = mapping.TargetField ?? column.SourceColumn;
                    column.IsIgnored = mapping.IsIgnored;
                    column.IsDuplicateKey = mapping.IsDuplicateKey;
                }
                else
                {
                    column.TargetField = column.SourceColumn;
                    column.IsIgnored = false;
                    column.IsDuplicateKey = false;
                }
            }

            foreach (var definition in profile.ValidationRules)
            {
                var column = ColumnProfiles.FirstOrDefault(candidate => string.Equals(
                    candidate.SourceColumn,
                    definition.SourceColumn,
                    StringComparison.OrdinalIgnoreCase));
                if (column is null)
                {
                    continue;
                }

                column.Severity = definition.Severity;
                switch (definition.Kind)
                {
                    case ValidationRuleKind.Required:
                        column.IsRequired = true;
                        break;
                    case ValidationRuleKind.Type:
                        column.ValidateType = true;
                        break;
                    case ValidationRuleKind.Email:
                        column.ValidateEmail = true;
                        break;
                    case ValidationRuleKind.Range:
                        column.MinimumAllowed = definition.Minimum;
                        column.MaximumAllowed = definition.Maximum;
                        break;
                    case ValidationRuleKind.AllowedValue:
                        column.AllowedValues = string.Join(", ", definition.AllowedValues);
                        break;
                    case ValidationRuleKind.Unique:
                        column.IsUnique = true;
                        break;
                }
            }

            foreach (var definition in profile.CleaningRules)
            {
                var column = ColumnProfiles.FirstOrDefault(candidate => string.Equals(
                    candidate.SourceColumn,
                    definition.SourceColumn,
                    StringComparison.OrdinalIgnoreCase));
                if (column is null)
                {
                    continue;
                }

                switch (definition.Kind)
                {
                    case CleaningRuleKind.Trim:
                        column.TrimText = true;
                        break;
                    case CleaningRuleKind.NormalizeWhitespace:
                        column.NormalizeWhitespace = true;
                        break;
                    case CleaningRuleKind.UpperCase:
                        column.CaseNormalization = TextCaseNormalization.Upper;
                        break;
                    case CleaningRuleKind.LowerCase:
                        column.CaseNormalization = TextCaseNormalization.Lower;
                        break;
                    case CleaningRuleKind.TitleCase:
                        column.CaseNormalization = TextCaseNormalization.Title;
                        break;
                    case CleaningRuleKind.NormalizeEmail:
                        column.NormalizeEmail = true;
                        break;
                    case CleaningRuleKind.NullTokens:
                        column.NullTokens = string.Join(", ", definition.Values);
                        break;
                    case CleaningRuleKind.CountryAlias:
                        column.CountryAliases = string.Join(
                            "; ",
                            definition.Aliases.Select(alias => $"{alias.Key}={alias.Value}"));
                        break;
                    case CleaningRuleKind.NormalizeDate:
                        column.NormalizeDate = true;
                        break;
                    case CleaningRuleKind.NormalizeDecimal:
                        column.NormalizeDecimal = true;
                        break;
                }
            }
        }
        finally
        {
            _suppressConfigurationRefresh = false;
        }

        ClearValidationResults();
        ClearCleaningResults();
        ClearDuplicateResults();
        RefreshPreview();
    }

    public async Task RunValidationAsync()
    {
        if (_dataset is null)
        {
            StatusMessage = "Import a file before running validation.";
            return;
        }

        ValidationRuleDefinition[] definitions;
        try
        {
            definitions = BuildValidationDefinitions();
        }
        catch (ArgumentException exception)
        {
            StatusMessage = exception.Message;
            return;
        }

        IsImportEnabled = false;
        StatusMessage = "Running validation…";
        try
        {
            var result = await Task.Run(() => validationService.ValidateAsync(
                _dataset,
                definitions,
                ValidationPass.BeforeCleaning,
                EffectiveCultureName));
            ApplyValidationResult(result);
            StatusMessage = result.Issues.Count == 0
                ? "Validation complete. No issues found."
                : "Validation complete. Review the Validation tab.";
            RefreshPreview();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            StatusMessage = $"Validation could not be completed: {exception.Message}";
        }
        finally
        {
            IsImportEnabled = true;
        }
    }

    public async Task RunCleaningAsync()
    {
        if (_dataset is null)
        {
            StatusMessage = "Import a file before running cleaning.";
            return;
        }

        CleaningRuleDefinition[] cleaningDefinitions;
        ValidationRuleDefinition[] validationDefinitions;
        try
        {
            cleaningDefinitions = BuildCleaningDefinitions();
            validationDefinitions = BuildValidationDefinitions();
        }
        catch (ArgumentException exception)
        {
            StatusMessage = exception.Message;
            return;
        }

        IsImportEnabled = false;
        StatusMessage = "Validating, cleaning and validating again…";
        try
        {
            var beforeValidation = await Task.Run(() => validationService.ValidateAsync(
                _dataset,
                validationDefinitions,
                ValidationPass.BeforeCleaning,
                EffectiveCultureName));
            var cleaningResult = await Task.Run(() => cleaningService.CleanAsync(
                _dataset,
                cleaningDefinitions,
                EffectiveCultureName));
            _dataset = cleaningResult.Dataset;
            await UpdateColumnProfilesAsync();
            var afterValidation = await Task.Run(() => validationService.ValidateAsync(
                _dataset,
                validationDefinitions,
                ValidationPass.AfterCleaning,
                EffectiveCultureName));
            ApplyValidationResult(afterValidation);

            var columnNames = _dataset.Columns.ToDictionary(column => column.Id, column => column.SourceName);
            CleaningChanges = cleaningResult.Changes.Select(change => new CleaningChangeViewModel(
                change.RowNumber,
                columnNames.GetValueOrDefault(change.ColumnId, "Unknown"),
                change.RuleCode,
                FormatValue(change.BeforeValue),
                FormatValue(change.AfterValue),
                change.Description)).ToArray();
            var beforeErrors = beforeValidation.Issues.Count(issue => issue.Severity == ValidationSeverity.Error);
            var afterErrors = afterValidation.Issues.Count(issue => issue.Severity == ValidationSeverity.Error);
            CleaningSummary = $"{cleaningResult.Changes.Count:N0} changes · validation errors: {beforeErrors:N0} before, {afterErrors:N0} after";
            StatusMessage = "Cleaning complete. Review the Cleaning and Validation tabs.";
            RefreshPreview();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            StatusMessage = $"Cleaning could not be completed: {exception.Message}";
        }
        finally
        {
            IsImportEnabled = true;
        }
    }

    public async Task RunDuplicateDetectionAsync()
    {
        if (_dataset is null)
        {
            StatusMessage = "Import a file before detecting duplicates.";
            return;
        }

        var keyColumns = ColumnProfiles.Where(column => column.IsDuplicateKey && !column.IsIgnored).ToArray();
        if (keyColumns.Length == 0)
        {
            StatusMessage = "Select at least one duplicate key column.";
            return;
        }

        IsImportEnabled = false;
        StatusMessage = "Detecting exact duplicates…";
        try
        {
            var definition = new DuplicateDefinition(keyColumns.Select(column => _dataset.Columns[column.ColumnIndex].Id));
            var result = await Task.Run(() => duplicateDetectionService.ResolveAsync(
                _dataset,
                definition,
                SelectedDuplicateAction));
            _dataset = result.Dataset;
            _duplicatesRemoved += result.RemovedRowNumbers.Count;
            DuplicateGroups = result.Detection.Groups.Select(group => new DuplicateGroupViewModel(
                group.GroupNumber,
                string.Join(", ", group.RowNumbers),
                string.Join(" | ", group.KeyValues.Select(value => FormatValue(value) ?? "∅")),
                group.RowNumbers.Count)).ToArray();
            DuplicateSummary = $"{result.Detection.Groups.Count:N0} groups · {result.Detection.DuplicateRowCount:N0} matching rows · {result.RemovedRowNumbers.Count:N0} removed";
            DatasetSummary = FormatDatasetSummary(_dataset);
            await UpdateColumnProfilesAsync();
            UpdateProcessingSummary();
            RefreshPreview();
            StatusMessage = "Duplicate detection complete. Review the Duplicates tab.";
        }
        catch (ArgumentException exception)
        {
            StatusMessage = $"Duplicate detection could not be completed: {exception.Message}";
        }
        finally
        {
            IsImportEnabled = true;
        }
    }

    public async Task ExportAsync(string filePath)
    {
        if (_dataset is null)
        {
            StatusMessage = "Import a file before exporting data.";
            return;
        }

        IsImportEnabled = false;
        StatusMessage = "Exporting a new output file…";
        try
        {
            var exportDataset = CreateExportDataset();
            var result = await Task.Run(() => exportService.ExportAsync(new ExportRequest(
                filePath,
                exportDataset,
                SelectedExportFilter)));
            try
            {
                await SaveHistoryAsync("Exported", Path.GetFileName(result.FilePath));
                await ReloadHistoryAsync();
                StatusMessage = $"Export complete: {result.ExportedRows:N0} rows written to {Path.GetFileName(result.FilePath)}.";
            }
            catch (DbException exception)
            {
                StatusMessage = $"Export complete, but local history could not be updated: {exception.Message}";
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            StatusMessage = $"Export could not be completed: {exception.Message}";
        }
        finally
        {
            IsImportEnabled = true;
        }
    }

    public async Task ExportErrorReportAsync(string filePath)
    {
        var errors = ValidationIssues
            .Where(issue => string.Equals(issue.Severity, ValidationSeverity.Error.ToString(), StringComparison.Ordinal))
            .Select(issue => new ErrorReportRow(
                issue.RowNumber,
                issue.ColumnName,
                issue.Rule,
                issue.Severity,
                issue.Message,
                issue.SourceValue))
            .ToArray();
        if (errors.Length == 0)
        {
            StatusMessage = "There are no rejected-row errors to export.";
            return;
        }

        try
        {
            await errorReportWriter.WriteAsync(filePath, errors);
            StatusMessage = $"Error report complete: {errors.Length:N0} issues written.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            StatusMessage = $"Error report could not be completed: {exception.Message}";
        }
    }

    private async Task ImportCoreAsync(string filePath, string? worksheetName)
    {
        var importCultureName = SelectedCultureName;
        var request = new ImportRequest(
            filePath,
            worksheetName,
            importCultureName,
            SelectedEncoding);
        var dataset = await Task.Run(() => importService.ImportAsync(request));
        _dataset = dataset;
        _datasetCultureName = importCultureName;
        OnPropertyChanged(nameof(EffectiveCultureName));
        _currentImportId = Guid.NewGuid();
        _importStartedAtUtc = DateTimeOffset.UtcNow;
        _sourceRowCount = dataset.Rows.Count;
        _duplicatesRemoved = 0;
        var profiles = await Task.Run(() => profilingService.Profile(dataset, EffectiveCultureName));
        ColumnProfiles.Clear();
        for (var index = 0; index < profiles.Count; index++)
        {
            ColumnProfiles.Add(new ColumnProfileViewModel(index, profiles[index], OnColumnConfigurationChanged));
        }

        OnPropertyChanged(nameof(HasColumnProfiles));
        ProfileName = SelectedProfile?.Name ?? Path.GetFileNameWithoutExtension(request.FilePath);
        if (SelectedProfile is not null)
        {
            ApplyProfileConfiguration(SelectedProfile);
        }
        else
        {
            ClearValidationResults();
            ClearCleaningResults();
            ClearDuplicateResults();
            RefreshPreview();
        }
        DatasetSummary = FormatDatasetSummary(dataset);
        UpdateProcessingSummary();
        await SaveHistoryAsync("Imported");
        await ReloadHistoryAsync();
        StatusMessage = "Import complete. The source file was not modified.";
    }

    private void RefreshPreview()
    {
        if (_dataset is not null)
        {
            Preview = CreatePreview(_dataset, ColumnProfiles, EffectiveCultureName);
        }
    }

    private static DataView CreatePreview(
        ImportedDataset dataset,
        IReadOnlyCollection<ColumnProfileViewModel> mappings,
        string cultureName)
    {
        var table = new DataTable(dataset.SourceName)
        {
            Locale = CultureInfo.GetCultureInfo(cultureName)
        };
        var visibleMappings = mappings.Where(mapping => !mapping.IsIgnored).ToArray();
        var usedNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        table.Columns.Add("Row status", typeof(string));
        foreach (var mapping in visibleMappings)
        {
            var baseName = string.IsNullOrWhiteSpace(mapping.TargetField)
                ? mapping.SourceColumn
                : mapping.TargetField.Trim();
            usedNames.TryGetValue(baseName, out var count);
            count++;
            usedNames[baseName] = count;
            table.Columns.Add(count == 1 ? baseName : $"{baseName} ({count})", typeof(object));
        }

        foreach (var importedRow in dataset.Rows.Take(PreviewRowLimit))
        {
            var row = table.NewRow();
            for (var index = 0; index < visibleMappings.Length; index++)
            {
                row[index + 1] = importedRow.Cells[visibleMappings[index].ColumnIndex].CurrentValue ?? DBNull.Value;
            }

            row[0] = importedRow.State == RowState.None ? "Not validated" : importedRow.State.ToString();

            table.Rows.Add(row);
        }

        return table.DefaultView;
    }

    private ValidationRuleDefinition[] BuildValidationDefinitions()
    {
        var definitions = new List<ValidationRuleDefinition>();
        foreach (var column in ColumnProfiles.Where(column => !column.IsIgnored))
        {
            if (column.ValidateType)
            {
                definitions.Add(new ValidationRuleDefinition(
                    column.SourceColumn,
                    ValidationRuleKind.Type,
                    column.Severity));
            }

            if (column.IsRequired)
            {
                definitions.Add(new ValidationRuleDefinition(
                    column.SourceColumn,
                    ValidationRuleKind.Required,
                    column.Severity));
            }

            if (column.ValidateEmail)
            {
                definitions.Add(new ValidationRuleDefinition(
                    column.SourceColumn,
                    ValidationRuleKind.Email,
                    column.Severity));
            }

            if (column.MinimumAllowed.HasValue || column.MaximumAllowed.HasValue)
            {
                definitions.Add(new ValidationRuleDefinition(
                    column.SourceColumn,
                    ValidationRuleKind.Range,
                    column.Severity,
                    column.MinimumAllowed,
                    column.MaximumAllowed));
            }

            var allowedValues = column.AllowedValues.Split(
                [',', ';', '|', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (allowedValues.Length > 0)
            {
                definitions.Add(new ValidationRuleDefinition(
                    column.SourceColumn,
                    ValidationRuleKind.AllowedValue,
                    column.Severity,
                    allowedValues: allowedValues));
            }

            if (column.IsUnique)
            {
                definitions.Add(new ValidationRuleDefinition(
                    column.SourceColumn,
                    ValidationRuleKind.Unique,
                    column.Severity));
            }
        }

        return definitions.ToArray();
    }

    private CleaningRuleDefinition[] BuildCleaningDefinitions()
    {
        var definitions = new List<CleaningRuleDefinition>();
        var order = 0;
        foreach (var column in ColumnProfiles.Where(column => !column.IsIgnored))
        {
            if (column.TrimText)
            {
                definitions.Add(new CleaningRuleDefinition(column.SourceColumn, CleaningRuleKind.Trim, order++));
            }

            if (column.NormalizeWhitespace)
            {
                definitions.Add(new CleaningRuleDefinition(
                    column.SourceColumn,
                    CleaningRuleKind.NormalizeWhitespace,
                    order++));
            }

            var nullTokens = SplitConfiguredValues(column.NullTokens);
            if (nullTokens.Length > 0)
            {
                definitions.Add(new CleaningRuleDefinition(
                    column.SourceColumn,
                    CleaningRuleKind.NullTokens,
                    order++,
                    nullTokens));
            }

            var caseRule = column.CaseNormalization switch
            {
                TextCaseNormalization.Upper => CleaningRuleKind.UpperCase,
                TextCaseNormalization.Lower => CleaningRuleKind.LowerCase,
                TextCaseNormalization.Title => CleaningRuleKind.TitleCase,
                _ => (CleaningRuleKind?)null
            };
            if (caseRule.HasValue)
            {
                definitions.Add(new CleaningRuleDefinition(column.SourceColumn, caseRule.Value, order++));
            }

            if (column.NormalizeEmail)
            {
                definitions.Add(new CleaningRuleDefinition(
                    column.SourceColumn,
                    CleaningRuleKind.NormalizeEmail,
                    order++));
            }

            var aliases = ParseAliases(column.SourceColumn, column.CountryAliases);
            if (aliases.Count > 0)
            {
                definitions.Add(new CleaningRuleDefinition(
                    column.SourceColumn,
                    CleaningRuleKind.CountryAlias,
                    order++,
                    aliases: aliases));
            }

            if (column.NormalizeDate)
            {
                definitions.Add(new CleaningRuleDefinition(
                    column.SourceColumn,
                    CleaningRuleKind.NormalizeDate,
                    order++));
            }

            if (column.NormalizeDecimal)
            {
                definitions.Add(new CleaningRuleDefinition(
                    column.SourceColumn,
                    CleaningRuleKind.NormalizeDecimal,
                    order++));
            }
        }

        return definitions.ToArray();
    }

    private static string[] SplitConfiguredValues(string value) => value.Split(
        [',', ';', '|', '\r', '\n'],
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static Dictionary<string, string> ParseAliases(string columnName, string value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in value.Split(
            [';', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = entry.IndexOf('=', StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex == entry.Length - 1)
            {
                throw new ArgumentException(
                    $"Country aliases for '{columnName}' must use the Alias=Canonical format.");
            }

            result[entry[..separatorIndex].Trim()] = entry[(separatorIndex + 1)..].Trim();
        }

        return result;
    }

    private void ApplyValidationResult(ValidationResult result)
    {
        if (_dataset is null)
        {
            return;
        }

        var columnNames = _dataset.Columns.ToDictionary(column => column.Id, column => column.SourceName);
        ValidationIssues = result.Issues.Select(issue => new ValidationIssueViewModel(
            issue.RowNumber,
            columnNames.GetValueOrDefault(issue.ColumnId, "Unknown"),
            issue.SourceValue,
            issue.RuleCode,
            issue.Severity.ToString(),
            issue.Message)).ToArray();
        RejectedRows = result.RejectedRows.Select(row => new RejectedRowViewModel(
            row.RowNumber,
            row.Issues.Count,
            string.Join(" | ", row.CurrentValues.Select(value => value?.ToString() ?? "∅"))))
            .ToArray();
        var errors = result.Issues.Count(issue => issue.Severity == ValidationSeverity.Error);
        var warnings = result.Issues.Count(issue => issue.Severity == ValidationSeverity.Warning);
        var infos = result.Issues.Count(issue => issue.Severity == ValidationSeverity.Info);
        ValidationSummary = $"{errors:N0} errors · {warnings:N0} warnings · {infos:N0} info · {result.RejectedRows.Count:N0} rejected rows";
        UpdateProcessingSummary();
    }

    private async Task UpdateColumnProfilesAsync()
    {
        if (_dataset is null)
        {
            return;
        }

        var profiles = await Task.Run(() => profilingService.Profile(_dataset, EffectiveCultureName));
        for (var index = 0; index < profiles.Count && index < ColumnProfiles.Count; index++)
        {
            ColumnProfiles[index].UpdateProfile(profiles[index]);
        }
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private void OnColumnConfigurationChanged()
    {
        if (_suppressConfigurationRefresh)
        {
            return;
        }

        ClearValidationResults();
        ClearCleaningResults();
        ClearDuplicateResults();
        RefreshPreview();
    }

    private void ClearValidationResults()
    {
        if (_dataset is not null)
        {
            foreach (var row in _dataset.Rows)
            {
                row.RemoveState(
                    RowState.Valid | RowState.Info | RowState.Warning | RowState.Invalid | RowState.Rejected);
            }
        }

        ValidationIssues = [];
        RejectedRows = [];
        ValidationSummary = "Validation has not been run for the current configuration.";
    }

    private void ClearCleaningResults()
    {
        CleaningChanges = [];
        CleaningSummary = "Cleaning has not been run for the current configuration.";
    }

    private void ClearDuplicateResults()
    {
        if (_dataset is not null)
        {
            foreach (var row in _dataset.Rows)
            {
                row.RemoveState(RowState.Duplicate);
            }
        }

        DuplicateGroups = [];
        DuplicateSummary = "Duplicate detection has not been run for the current configuration.";
    }

    private async Task ReloadProfilesAsync(Guid? selectedId = null)
    {
        SavedProfiles = await profileRepository.GetAllAsync();
        SelectedProfile = selectedId.HasValue
            ? SavedProfiles.FirstOrDefault(profile => profile.Id == selectedId.Value)
            : null;
    }

    private ImportedDataset CreateExportDataset()
    {
        var selected = ColumnProfiles.Where(column => !column.IsIgnored).ToArray();
        var columns = selected.Select((mapping, index) => new ImportedColumn(
            Guid.NewGuid(),
            index,
            string.IsNullOrWhiteSpace(mapping.TargetField) ? mapping.SourceColumn : mapping.TargetField.Trim(),
            _dataset!.Columns[mapping.ColumnIndex].DataType,
            _dataset.Columns[mapping.ColumnIndex].SemanticType)).ToArray();
        var rows = _dataset!.Rows.Select(sourceRow =>
        {
            var row = new ImportedRow(sourceRow.SourceRowNumber, selected.Select((mapping, index) =>
            {
                var sourceCell = sourceRow.Cells[mapping.ColumnIndex];
                return new DataCell(columns[index].Id, sourceCell.SourceValue, sourceCell.CurrentValue);
            }));
            row.AddState(sourceRow.State);
            return row;
        }).ToArray();
        return new ImportedDataset(_dataset.SourceName, columns, rows);
    }

    private async Task SaveHistoryAsync(string status, string? outputFileName = null)
    {
        if (_dataset is null || _currentImportId == Guid.Empty)
        {
            return;
        }

        var invalid = _dataset.Rows.Count(row => row.State.HasFlag(RowState.Invalid) || row.State.HasFlag(RowState.Rejected));
        var modified = _dataset.Rows.Count(row => row.State.HasFlag(RowState.Modified));
        await historyRepository.SaveAsync(new ImportHistoryEntry(
            _currentImportId,
            _dataset.SourceName,
            _importStartedAtUtc,
            status == "Exported" ? DateTimeOffset.UtcNow : null,
            _sourceRowCount,
            invalid,
            status,
            _dataset.Rows.Count - invalid,
            modified,
            _duplicatesRemoved,
            outputFileName));
    }

    private async Task ReloadHistoryAsync() =>
        HistoryEntries = await historyRepository.GetRecentAsync(25);

    private void UpdateProcessingSummary()
    {
        if (_dataset is null)
        {
            ProcessingSummary = "No active import.";
            return;
        }

        var invalid = _dataset.Rows.Count(row => row.State.HasFlag(RowState.Invalid) || row.State.HasFlag(RowState.Rejected));
        var modified = _dataset.Rows.Count(row => row.State.HasFlag(RowState.Modified));
        ProcessingSummary = $"Source {_sourceRowCount:N0} · working {_dataset.Rows.Count:N0} · valid {_dataset.Rows.Count - invalid:N0} · invalid {invalid:N0} · modified {modified:N0} · duplicates removed {_duplicatesRemoved:N0}";
    }

    private static string FormatDatasetSummary(ImportedDataset dataset)
    {
        var previewRows = Math.Min(dataset.Rows.Count, PreviewRowLimit);
        var previewText = dataset.Rows.Count > PreviewRowLimit
            ? $" · preview {previewRows:N0} of {dataset.Rows.Count:N0} rows"
            : string.Empty;
        return $"{dataset.SourceName} · {dataset.Rows.Count:N0} rows · {dataset.Columns.Count:N0} columns{previewText}";
    }

    private static string[] BuildAvailableCultureNames()
    {
        return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => culture.Name)
            .Append(GetDefaultCultureName())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetDefaultCultureName() => string.IsNullOrWhiteSpace(CultureInfo.CurrentCulture.Name)
        ? "en-US"
        : CultureInfo.CurrentCulture.Name;

    private static string NormalizeCultureName(string? cultureName) =>
        CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(cultureName) ? GetDefaultCultureName() : cultureName).Name;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
