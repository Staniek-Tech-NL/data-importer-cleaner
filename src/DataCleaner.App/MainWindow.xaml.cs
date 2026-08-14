using System.Windows;
using Microsoft.Win32;

namespace DataCleaner.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void SelectFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select a data file",
            Filter = "Supported files (*.csv;*.xlsx)|*.csv;*.xlsx|CSV files (*.csv)|*.csv|Excel workbooks (*.xlsx)|*.xlsx",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            await _viewModel.PrepareFileAsync(dialog.FileName);
        }
    }

    private async void ImportWorksheet_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.ImportSelectedWorksheetAsync();

    private async void SaveProfile_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveProfileAsync();

    private void ApplyProfile_Click(object sender, RoutedEventArgs e) =>
        _viewModel.ApplySelectedProfile();

    private async void RunValidation_Click(object sender, RoutedEventArgs e) =>
        await _viewModel.RunValidationAsync();
}
