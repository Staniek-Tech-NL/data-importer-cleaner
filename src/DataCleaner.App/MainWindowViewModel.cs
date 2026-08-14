using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Runtime.CompilerServices;
using DataCleaner.Application.Abstractions;
using DataCleaner.Application.Profiling;
using DataCleaner.Domain.Data;
using DataCleaner.Domain.Profiles;

namespace DataCleaner.App;

public sealed class MainWindowViewModel(
    IDataImportService importService,
    IDataProfilingService profilingService,
    IImportProfileRepository profileRepository) : INotifyPropertyChanged
{
    private string _statusMessage = "Select a CSV file to inspect its contents safely.";
    private DataView? _preview;
    private string? _datasetSummary;
    private bool _isImportEnabled = true;
    private IReadOnlyList<string> _worksheetNames = [];
    private string? _selectedWorksheet;
    private string _selectedEncoding = "UTF-8";
    private string? _pendingFilePath;
    private ImportedDataset? _dataset;
    private IReadOnlyList<ImportProfile> _savedProfiles = [];
    private ImportProfile? _selectedProfile;
    private string _profileName = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ApplicationName => "Data Importer & Cleaner";

    public IReadOnlyList<string> AvailableEncodings { get; } = ["UTF-8", "Windows-1250"];

    public string SelectedEncoding
    {
        get => _selectedEncoding;
        set => SetField(ref _selectedEncoding, value);
    }

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
                column.IsIgnored))
            .ToArray();
        var profile = SelectedProfile is not null
            && string.Equals(SelectedProfile.Name, ProfileName.Trim(), StringComparison.OrdinalIgnoreCase)
                ? SelectedProfile
                : SavedProfiles.FirstOrDefault(candidate => string.Equals(
                    candidate.Name,
                    ProfileName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                    ?? new ImportProfile(ProfileName, CultureInfo.CurrentCulture.Name, mappings);

        if (profile.ColumnMappings.Count > 0)
        {
            profile.Rename(ProfileName);
            profile.UpdateConfiguration(CultureInfo.CurrentCulture.Name, mappings);
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

        var mappings = SelectedProfile.ColumnMappings.ToDictionary(
            mapping => mapping.SourceColumn,
            StringComparer.OrdinalIgnoreCase);
        foreach (var column in ColumnProfiles)
        {
            if (mappings.TryGetValue(column.SourceColumn, out var mapping))
            {
                column.TargetField = mapping.TargetField ?? column.SourceColumn;
                column.IsIgnored = mapping.IsIgnored;
            }
            else
            {
                column.TargetField = column.SourceColumn;
                column.IsIgnored = false;
            }
        }

        RefreshPreview();
        StatusMessage = $"Profile '{SelectedProfile.Name}' version {SelectedProfile.ProfileVersion} applied.";
    }

    private async Task ImportCoreAsync(string filePath, string? worksheetName)
    {
        var request = new ImportRequest(
            filePath,
            worksheetName,
            CultureInfo.CurrentCulture.Name,
            SelectedEncoding);
        var dataset = await importService.ImportAsync(request);
        _dataset = dataset;
        var profiles = profilingService.Profile(dataset, CultureInfo.CurrentCulture.Name);
        ColumnProfiles.Clear();
        for (var index = 0; index < profiles.Count; index++)
        {
            ColumnProfiles.Add(new ColumnProfileViewModel(index, profiles[index], RefreshPreview));
        }

        OnPropertyChanged(nameof(HasColumnProfiles));
        ProfileName = Path.GetFileNameWithoutExtension(request.FilePath);
        SelectedProfile = null;
        RefreshPreview();
        DatasetSummary = $"{dataset.SourceName} · {dataset.Rows.Count:N0} rows · {dataset.Columns.Count:N0} columns";
        StatusMessage = "Import complete. The source file was not modified.";
    }

    private void RefreshPreview()
    {
        if (_dataset is not null)
        {
            Preview = CreatePreview(_dataset, ColumnProfiles);
        }
    }

    private static DataView CreatePreview(
        ImportedDataset dataset,
        IReadOnlyCollection<ColumnProfileViewModel> mappings)
    {
        var table = new DataTable(dataset.SourceName)
        {
            Locale = CultureInfo.CurrentCulture
        };
        var visibleMappings = mappings.Where(mapping => !mapping.IsIgnored).ToArray();
        var usedNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

        foreach (var importedRow in dataset.Rows)
        {
            var row = table.NewRow();
            for (var index = 0; index < visibleMappings.Length; index++)
            {
                row[index] = importedRow.Cells[visibleMappings[index].ColumnIndex].CurrentValue ?? DBNull.Value;
            }

            table.Rows.Add(row);
        }

        return table.DefaultView;
    }

    private async Task ReloadProfilesAsync(Guid? selectedId = null)
    {
        SavedProfiles = await profileRepository.GetAllAsync();
        SelectedProfile = selectedId.HasValue
            ? SavedProfiles.FirstOrDefault(profile => profile.Id == selectedId.Value)
            : null;
    }

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
