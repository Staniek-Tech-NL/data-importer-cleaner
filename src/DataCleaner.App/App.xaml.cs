using System.Windows;
using DataCleaner.Application;
using DataCleaner.Infrastructure;
using DataCleaner.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataCleaner.App;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
            })
            .ConfigureServices(services =>
            {
                services.AddApplication();
                services.AddInfrastructure();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await _host.StartAsync();

            await using var scope = _host.Services.CreateAsyncScope();
            var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await databaseInitializer.InitializeAsync();

            var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            await viewModel.InitializeAsync();

            _host.Services.GetRequiredService<MainWindow>().Show();
        }
        catch (Exception exception)
        {
            var logger = _host.Services.GetService<ILogger<App>>();
            if (logger is not null)
            {
                LogStartupFailure(logger, exception);
            }
            MessageBox.Show(
                "The application could not start. Technical details were written to the diagnostic output.",
                "Data Importer & Cleaner",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync(TimeSpan.FromSeconds(5));
        _host.Dispose();
        base.OnExit(e);
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "Application startup failed.")]
    private static partial void LogStartupFailure(ILogger logger, Exception exception);
}
