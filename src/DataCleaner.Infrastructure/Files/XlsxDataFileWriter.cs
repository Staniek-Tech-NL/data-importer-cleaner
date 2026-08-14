using DataCleaner.Application.Abstractions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DataCleaner.Infrastructure.Files;

public sealed class XlsxDataFileWriter : IDataFileWriter
{
    public bool CanWrite(string fileExtension) =>
        string.Equals(fileExtension, ".xlsx", StringComparison.OrdinalIgnoreCase);

    public Task WriteAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        CsvDataFileWriter.EnsureNewFile(request.FilePath);
        using var document = SpreadsheetDocument.Create(request.FilePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        worksheetPart.Worksheet = new Worksheet(sheetData);
        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Export"
        });
        sheetData.Append(CreateRow(request.Dataset.Columns.Select(column => column.SourceName)));
        foreach (var row in request.Dataset.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sheetData.Append(CreateRow(row.Cells.Select(cell => ExportValueFormatter.Format(cell.CurrentValue))));
        }

        workbookPart.Workbook.Save();
        return Task.CompletedTask;
    }

    private static Row CreateRow(IEnumerable<string> values)
    {
        var row = new Row();
        foreach (var value in values)
        {
            row.Append(new Cell
            {
                DataType = CellValues.InlineString,
                InlineString = new InlineString(new Text(value) { Space = SpaceProcessingModeValues.Preserve })
            });
        }

        return row;
    }
}
