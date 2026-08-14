using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Data;
using DataCleaner.Infrastructure.Files;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Data.Sqlite;

namespace DataCleaner.Infrastructure.Tests;

public sealed class DataFileWriterTests
{
    [Fact]
    public async Task CsvWriter_EscapesValuesAndNeverOverwritesExistingFile()
    {
        var path = TemporaryPath("csv");
        try
        {
            await new CsvDataFileWriter().WriteAsync(new ExportRequest(path, Dataset()));
            var text = await File.ReadAllTextAsync(path);

            Assert.Contains("Name,Amount", text);
            Assert.Contains("\"Doe, Jane\",12.5", text);
            await Assert.ThrowsAsync<IOException>(() =>
                new CsvDataFileWriter().WriteAsync(new ExportRequest(path, Dataset())));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task XlsxWriter_CreatesReadableWorkbook()
    {
        var path = TemporaryPath("xlsx");
        try
        {
            await new XlsxDataFileWriter().WriteAsync(new ExportRequest(path, Dataset()));
            using var document = SpreadsheetDocument.Open(path, false);
            var workbookPart = Assert.IsType<WorkbookPart>(document.WorkbookPart);
            var workbook = Assert.IsType<DocumentFormat.OpenXml.Spreadsheet.Workbook>(workbookPart.Workbook);
            var sheets = Assert.IsType<DocumentFormat.OpenXml.Spreadsheet.Sheets>(workbook.Sheets);

            Assert.Single(sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>());
            Assert.NotNull(workbookPart.WorksheetParts.Single().Worksheet);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SqliteWriter_CreatesDataTableWithCurrentValues()
    {
        var path = TemporaryPath("sqlite");
        try
        {
            await new SqliteDataFileWriter().WriteAsync(new ExportRequest(path, Dataset()));
            await using var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Name, Amount FROM data";
            await using var reader = await command.ExecuteReaderAsync();

            Assert.True(await reader.ReadAsync());
            Assert.Equal("Doe, Jane", reader.GetString(0));
            Assert.Equal("12.5", reader.GetString(1));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ErrorReportWriter_CreatesIndependentCsv()
    {
        var path = TemporaryPath("csv");
        try
        {
            await new CsvErrorReportWriter().WriteAsync(path,
            [
                new ErrorReportRow(2, "Email", "Email", "Error", "Invalid, email", "bad")
            ]);
            var text = await File.ReadAllTextAsync(path);

            Assert.Contains("Source row,Column,Rule", text);
            Assert.Contains("\"Invalid, email\"", text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static ImportedDataset Dataset()
    {
        var name = new ImportedColumn(0, "Name");
        var amount = new ImportedColumn(1, "Amount");
        return new ImportedDataset("source.csv", [name, amount],
        [
            new ImportedRow(2,
            [
                new DataCell(name.Id, "Doe, Jane", "Doe, Jane"),
                new DataCell(amount.Id, "12,5", 12.5m)
            ])
        ]);
    }

    private static string TemporaryPath(string extension) =>
        Path.Combine(Path.GetTempPath(), $"DataCleaner-{Guid.NewGuid():N}.{extension}");
}
