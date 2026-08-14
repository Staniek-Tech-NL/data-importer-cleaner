using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Data;
using DataCleaner.Infrastructure.Files;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DataCleaner.Infrastructure.Tests;

public sealed class XlsxDataFileReaderTests
{
    private readonly XlsxDataFileReader _reader = new();

    [Fact]
    public void CanRead_AcceptsXlsxExtensionIgnoringCase()
    {
        Assert.True(_reader.CanRead(".xlsx"));
        Assert.True(_reader.CanRead(".XLSX"));
        Assert.False(_reader.CanRead(".xls"));
    }

    [Fact]
    public async Task GetWorksheetNamesAsync_ReturnsWorksheetsInWorkbookOrder()
    {
        var filePath = CreateWorkbook();

        try
        {
            var names = await _reader.GetWorksheetNamesAsync(filePath);

            Assert.Equal(["Customers", "Archive"], names);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadAsync_UsesSelectedWorksheetAndDetectsFirstNonEmptyHeaderRow()
    {
        var filePath = CreateWorkbook();

        try
        {
            var dataset = await _reader.ReadAsync(new ImportRequest(filePath, "Customers", "en-US"));

            Assert.EndsWith(".xlsx [Customers]", dataset.SourceName, StringComparison.Ordinal);
            Assert.Equal(["Name", "Age", "Joined", "Active"],
                dataset.Columns.Select(column => column.SourceName));
            Assert.Equal([DataType.Text, DataType.Integer, DataType.Date, DataType.Boolean],
                dataset.Columns.Select(column => column.DataType));
            Assert.Single(dataset.Rows);
            Assert.Equal(5, dataset.Rows[0].SourceRowNumber);
            Assert.Equal("Jane", dataset.Rows[0].Cells[0].CurrentValue);
            Assert.Equal(42L, dataset.Rows[0].Cells[1].CurrentValue);
            Assert.Equal(new DateTime(2026, 8, 14), dataset.Rows[0].Cells[2].CurrentValue);
            Assert.Equal(true, dataset.Rows[0].Cells[3].CurrentValue);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ReadAsync_RejectsUnknownWorksheet()
    {
        var filePath = CreateWorkbook();

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _reader.ReadAsync(new ImportRequest(filePath, "Missing", "en-US")));

            Assert.Contains("Missing", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task GetWorksheetNamesAsync_RejectsInvalidWorkbookWithFriendlyError()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");
        await File.WriteAllTextAsync(filePath, "not a workbook");

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _reader.GetWorksheetNamesAsync(filePath));

            Assert.Contains("valid XLSX", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateWorkbook()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DataCleaner.Tests");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"{Guid.NewGuid():N}.xlsx");

        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet = new Stylesheet(
            new Fonts(new Font()),
            new Fills(new Fill()),
            new Borders(new Border()),
            new CellStyleFormats(new CellFormat()),
            new CellFormats(
                new CellFormat(),
                new CellFormat { NumberFormatId = 14U, ApplyNumberFormat = true }));

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        var customersPart = workbookPart.AddNewPart<WorksheetPart>();
        customersPart.Worksheet = new Worksheet(new SheetData(
            new Row { RowIndex = 2U },
            new Row(
                TextCell("A3", "Name"),
                TextCell("B3", "Age"),
                TextCell("C3", "Joined"),
                TextCell("D3", "Active"))
            { RowIndex = 3U },
            new Row { RowIndex = 4U },
            new Row(
                TextCell("A5", "Jane"),
                NumberCell("B5", "42"),
                NumberCell("C5", new DateTime(2026, 8, 14).ToOADate().ToString(System.Globalization.CultureInfo.InvariantCulture), 1U),
                BooleanCell("D5", true))
            { RowIndex = 5U }));
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(customersPart),
            SheetId = 1U,
            Name = "Customers"
        });

        var archivePart = workbookPart.AddNewPart<WorksheetPart>();
        archivePart.Worksheet = new Worksheet(new SheetData(
            new Row(TextCell("A1", "Reference")) { RowIndex = 1U },
            new Row(TextCell("A2", "A-001")) { RowIndex = 2U }));
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(archivePart),
            SheetId = 2U,
            Name = "Archive"
        });

        workbookPart.Workbook.Save();
        return filePath;
    }

    private static Cell TextCell(string reference, string value) => new()
    {
        CellReference = reference,
        DataType = CellValues.String,
        CellValue = new CellValue(value)
    };

    private static Cell NumberCell(string reference, string value, uint? styleIndex = null) => new()
    {
        CellReference = reference,
        DataType = CellValues.Number,
        StyleIndex = styleIndex,
        CellValue = new CellValue(value)
    };

    private static Cell BooleanCell(string reference, bool value) => new()
    {
        CellReference = reference,
        DataType = CellValues.Boolean,
        CellValue = new CellValue(value ? "1" : "0")
    };
}
