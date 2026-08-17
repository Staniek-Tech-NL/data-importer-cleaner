using System.IO;
using System.Windows;

namespace DataCleaner.PortfolioCapture;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var options = CaptureOptions.Parse(args);
        Exception? failure = null;
        var application = new System.Windows.Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };

        application.Startup += async (_, _) =>
        {
            try
            {
                await PortfolioCaptureRunner.RunAsync(options);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                application.Shutdown(failure is null ? 0 : 1);
            }
        };

        application.Run();
        if (failure is null)
        {
            return 0;
        }

        Console.Error.WriteLine(failure);
        return 1;
    }
}

internal sealed record CaptureOptions(string DemoFile, string OutputDirectory, string DataDirectory)
{
    public static CaptureOptions Parse(IReadOnlyList<string> args)
    {
        var demoFile = GetValue(args, "--demo");
        var outputDirectory = GetValue(args, "--capture-portfolio");
        var dataDirectory = GetValue(args, "--data-directory");

        if (demoFile is null || outputDirectory is null || dataDirectory is null)
        {
            throw new ArgumentException(
                "Usage: --demo <csv-path> --capture-portfolio <output-directory> --data-directory <isolated-data-directory>");
        }

        var resolvedDemoFile = Path.GetFullPath(demoFile);
        if (!File.Exists(resolvedDemoFile))
        {
            throw new FileNotFoundException("The demo CSV file was not found.", resolvedDemoFile);
        }

        return new CaptureOptions(
            resolvedDemoFile,
            Path.GetFullPath(outputDirectory),
            Path.GetFullPath(dataDirectory));
    }

    private static string? GetValue(IReadOnlyList<string> args, string option)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
