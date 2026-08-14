using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Data;

namespace DataCleaner.App;

public sealed class MainWindowViewModel(IDataImportService importService) : INotifyPropertyChanged
{
    private string _statusMessage = "Select a CSV file to inspect its contents safely.";
    private DataView? _preview;
    private string? _datasetSummary;
    private bool _isImportEnabled = true;
    private IReadOnlyList<string> _worksheetNames = [];
    private string? _selectedWorksheet;
    private string _selectedEncoding = "UTF-8";
    private string? _pendingFilePath;

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

    private async Task ImportCoreAsync(string filePath, string? worksheetName)
    {
        var request = new ImportRequest(
            filePath,
            worksheetName,
            CultureInfo.CurrentCulture.Name,
            SelectedEncoding);
        var dataset = await importService.ImportAsync(request);
        Preview = CreatePreview(dataset);
        DatasetSummary = $"{dataset.SourceName} · {dataset.Rows.Count:N0} rows · {dataset.Columns.Count:N0} columns";
        StatusMessage = "Import complete. The source file was not modified.";
    }

    private static DataView CreatePreview(ImportedDataset dataset)
    {
        var table = new DataTable(dataset.SourceName)
        {
            Locale = CultureInfo.CurrentCulture
        };
        foreach (var column in dataset.Columns)
        {
            table.Columns.Add(column.SourceName, typeof(object));
        }

        foreach (var importedRow in dataset.Rows)
        {
            var row = table.NewRow();
            for (var index = 0; index < dataset.Columns.Count; index++)
            {
                row[index] = importedRow.Cells[index].CurrentValue ?? DBNull.Value;
            }

            table.Rows.Add(row);
        }

        return table.DefaultView;
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
