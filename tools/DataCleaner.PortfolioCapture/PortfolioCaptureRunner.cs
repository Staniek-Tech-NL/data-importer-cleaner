using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DataCleaner.App;
using DataCleaner.Application;
using DataCleaner.Domain.Cleaning;
using DataCleaner.Domain.Duplicates;
using DataCleaner.Infrastructure;
using DataCleaner.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DataCleaner.PortfolioCapture;

internal static class PortfolioCaptureRunner
{
    private const double CaptureWidth = 1920;
    private const double CaptureHeight = 1080;

    public static async Task RunAsync(CaptureOptions options)
    {
        Directory.CreateDirectory(options.OutputDirectory);
        Directory.CreateDirectory(options.DataDirectory);

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(Path.Combine(options.DataDirectory, "portfolio-capture.db"));
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
        }

        var viewModel = provider.GetRequiredService<MainWindowViewModel>();
        await viewModel.InitializeAsync();

        var window = provider.GetRequiredService<MainWindow>();
        window.Width = CaptureWidth;
        window.Height = CaptureHeight;
        window.WindowState = WindowState.Normal;
        window.Show();

        try
        {
            await StabilizeLayoutAsync(window);
            var startupWarmUpPath = Path.Combine(options.OutputDirectory, ".startup-warmup.png");
            Capture(window, startupWarmUpPath);
            await StabilizeLayoutAsync(window);
            File.Delete(startupWarmUpPath);

            await viewModel.PrepareFileAsync(options.DemoFile);
            ConfigureDemoWorkflow(viewModel);

            await SelectTabAsync(window, "Profile & mapping");
            await CaptureWithWarmUpAsync(window, Path.Combine(options.OutputDirectory, "data-profiling.png"));

            await viewModel.RunValidationAsync();
            await viewModel.RunCleaningAsync();
            await SelectTabAsync(window, "Cleaning");
            await CaptureWithWarmUpAsync(window, Path.Combine(options.OutputDirectory, "cleaning-before-after.png"));

            viewModel.SelectedDuplicateAction = DuplicateResolutionAction.MarkForReview;
            await viewModel.RunDuplicateDetectionAsync();
            var exportPath = Path.Combine(options.DataDirectory, "portfolio-demo-output.csv");
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }

            await viewModel.ExportAsync(exportPath);
            await SelectTabAsync(window, "Export & history");
            await CaptureWithWarmUpAsync(window, Path.Combine(options.OutputDirectory, "import-summary.png"));
        }
        finally
        {
            window.Close();
        }
    }

    private static void ConfigureDemoWorkflow(MainWindowViewModel viewModel)
    {
        foreach (var column in viewModel.ColumnProfiles)
        {
            column.TrimText = true;
            column.NormalizeWhitespace = true;

            switch (column.SourceColumn)
            {
                case "Customer ID":
                    column.TargetField = "CustomerId";
                    column.IsRequired = true;
                    column.IsUnique = true;
                    break;
                case "Full Name":
                    column.TargetField = "CustomerName";
                    column.IsRequired = true;
                    column.CaseNormalization = TextCaseNormalization.Title;
                    break;
                case "Email":
                    column.TargetField = "EmailAddress";
                    column.IsRequired = true;
                    column.ValidateEmail = true;
                    column.NormalizeEmail = true;
                    column.IsDuplicateKey = true;
                    break;
                case "Country":
                    column.TargetField = "Country";
                    column.IsRequired = true;
                    column.CountryAliases = "PL=Poland; NL=Netherlands; IT=Italy; UK=United Kingdom; DE=Germany; BE=Belgium";
                    column.IsDuplicateKey = true;
                    break;
                case "Signup Date":
                    column.TargetField = "SignupDate";
                    column.NormalizeDate = true;
                    break;
                case "Revenue":
                    column.TargetField = "AnnualRevenue";
                    column.MinimumAllowed = 0;
                    column.NormalizeDecimal = true;
                    break;
                case "Notes":
                    column.NullTokens = "N/A; NULL";
                    break;
            }
        }
    }

    private static async Task SelectTabAsync(MainWindow window, string header)
    {
        var tabControl = FindVisualChildren<TabControl>(window).FirstOrDefault()
            ?? throw new InvalidOperationException("The workflow tab control was not found.");
        var tab = tabControl.Items
            .OfType<TabItem>()
            .FirstOrDefault(item => string.Equals(item.Header?.ToString(), header, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"The '{header}' tab was not found.");

        tabControl.SelectedItem = tab;
        await StabilizeLayoutAsync(window);
    }

    private static async Task CaptureWithWarmUpAsync(MainWindow window, string outputPath)
    {
        await StabilizeLayoutAsync(window);
        var warmUpPath = Path.Combine(Path.GetDirectoryName(outputPath)!, ".capture-warmup.png");
        Capture(window, warmUpPath);
        await StabilizeLayoutAsync(window);
        Capture(window, outputPath);
        File.Delete(warmUpPath);
    }

    private static async Task StabilizeLayoutAsync(Window window)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(150);
        window.UpdateLayout();
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
    }

    private static void Capture(Window window, string outputPath)
    {
        if (window.Content is not FrameworkElement root)
        {
            throw new InvalidOperationException("The main window does not have renderable content.");
        }

        root.UpdateLayout();
        var width = Math.Max(1, (int)Math.Ceiling(root.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(root.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
