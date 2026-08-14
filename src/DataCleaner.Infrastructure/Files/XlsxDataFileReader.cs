using System.Globalization;
using System.Text.RegularExpressions;
using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Data;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DataCleaner.Infrastructure.Files;

public sealed partial class XlsxDataFileReader : IWorksheetFileReader
{
    private static readonly HashSet<uint> BuiltInDateFormatIds =
        [14, 15, 16, 17, 18, 19, 20, 21, 22, 27, 30, 36, 45, 46, 47, 50, 57];

    public bool CanRead(string fileExtension) =>
        string.Equals(fileExtension, ".xlsx", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<string>> GetWorksheetNamesAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var document = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = document.WorkbookPart
                ?? throw new InvalidDataException("The XLSX workbook structure is incomplete.");
            IReadOnlyList<string> names = GetWorksheetSheets(workbookPart)
                .Select(sheet => sheet.Name?.Value)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToArray();
            return Task.FromResult(names);
        }
        catch (OpenXmlPackageException exception)
        {
            throw new InvalidDataException("The selected file is not a valid XLSX workbook.", exception);
        }
        catch (FileFormatException exception)
        {
            throw new InvalidDataException("The selected file is not a valid XLSX workbook.", exception);
        }
    }

    public Task<ImportedDataset> ReadAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateFile(request.FilePath);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var document = SpreadsheetDocument.Open(request.FilePath, false);
            var workbookPart = document.WorkbookPart
                ?? throw new InvalidDataException("The XLSX workbook structure is incomplete.");
            var sheets = GetWorksheetSheets(workbookPart);
            if (sheets.Count == 0)
            {
                throw new InvalidDataException("The XLSX workbook does not contain a worksheet.");
            }

            var sheet = string.IsNullOrWhiteSpace(request.WorksheetName)
                ? sheets[0]
                : sheets.FirstOrDefault(candidate => string.Equals(
                    candidate.Name?.Value,
                    request.WorksheetName,
                    StringComparison.Ordinal));
            if (sheet is null)
            {
                throw new InvalidDataException($"Worksheet '{request.WorksheetName}' was not found.");
            }

            var relationshipId = sheet.Id?.Value
                ?? throw new InvalidDataException("The selected worksheet has no relationship identifier.");
            if (workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart)
            {
                throw new InvalidDataException("The selected workbook item is not a worksheet.");
            }

            var rows = ReadRows(workbookPart, worksheetPart, cancellationToken);
            var headerIndex = rows.FindIndex(row => row.Cells.Count > 0);
            if (headerIndex < 0)
            {
                throw new InvalidDataException("The selected worksheet is empty.");
            }

            var relevantRows = rows.Skip(headerIndex).Where(row => row.Cells.Count > 0).ToArray();
            var columnCount = relevantRows.SelectMany(row => row.Cells.Keys).DefaultIfEmpty(-1).Max() + 1;
            var header = relevantRows[0];
            var headers = CreateUniqueHeaders(Enumerable.Range(0, columnCount)
                .Select(index => header.Cells.TryGetValue(index, out var cell) ? cell.DisplayValue : null)
                .ToArray());
            var dataRows = relevantRows.Skip(1).ToArray();
            var types = Enumerable.Range(0, columnCount)
                .Select(index => InferType(dataRows.Select(row =>
                    row.Cells.TryGetValue(index, out var cell) ? cell.ParsedValue : null)))
                .ToArray();
            var columns = headers
                .Select((name, index) => new ImportedColumn(Guid.NewGuid(), index, name, types[index]))
                .ToArray();
            var importedRows = dataRows
                .Select(row => CreateImportedRow(row, columns))
                .ToArray();
            var sourceName = $"{Path.GetFileName(request.FilePath)} [{sheet.Name?.Value}]";

            return Task.FromResult(new ImportedDataset(sourceName, columns, importedRows));
        }
        catch (OpenXmlPackageException exception)
        {
            throw new InvalidDataException("The selected file is not a valid XLSX workbook.", exception);
        }
        catch (FileFormatException exception)
        {
            throw new InvalidDataException("The selected file is not a valid XLSX workbook.", exception);
        }
    }

    private static List<Sheet> GetWorksheetSheets(WorkbookPart workbookPart)
    {
        var workbook = workbookPart.Workbook
            ?? throw new InvalidDataException("The XLSX workbook structure is incomplete.");
        return workbook.Sheets?
            .Elements<Sheet>()
            .Where(sheet => sheet.Id?.Value is { } id && workbookPart.GetPartById(id) is WorksheetPart)
            .ToList() ?? [];
    }

    private static List<WorksheetRow> ReadRows(
        WorkbookPart workbookPart,
        WorksheetPart worksheetPart,
        CancellationToken cancellationToken)
    {
        var workbook = workbookPart.Workbook
            ?? throw new InvalidDataException("The XLSX workbook structure is incomplete.");
        var worksheet = worksheetPart.Worksheet
            ?? throw new InvalidDataException("The selected worksheet structure is incomplete.");
        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
        var stylesheet = workbookPart.WorkbookStylesPart?.Stylesheet;
        var uses1904Dates = workbook.WorkbookProperties?.Date1904?.Value ?? false;
        var result = new List<WorksheetRow>();
        var fallbackRowNumber = 1L;

        foreach (var row in worksheet.Descendants<Row>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rowNumber = row.RowIndex?.Value ?? (uint)fallbackRowNumber;
            fallbackRowNumber = rowNumber + 1L;
            var cells = new Dictionary<int, WorksheetCell>();
            var fallbackColumnIndex = 0;

            foreach (var cell in row.Elements<Cell>())
            {
                var columnIndex = cell.CellReference?.Value is { } reference
                    ? GetColumnIndex(reference)
                    : fallbackColumnIndex;
                fallbackColumnIndex = columnIndex + 1;
                var value = ReadCell(cell, sharedStrings, stylesheet, uses1904Dates);
                if (value is not null)
                {
                    cells[columnIndex] = value;
                }
            }

            result.Add(new WorksheetRow(rowNumber, cells));
        }

        return result;
    }

    private static WorksheetCell? ReadCell(
        Cell cell,
        SharedStringTable? sharedStrings,
        Stylesheet? stylesheet,
        bool uses1904Dates)
    {
        var rawValue = cell.CellValue?.InnerText;
        var dataType = cell.DataType?.Value;

        if (dataType == CellValues.InlineString)
        {
            var value = cell.InlineString?.InnerText;
            return string.IsNullOrEmpty(value) ? null : new WorksheetCell(value, value);
        }

        if (string.IsNullOrEmpty(rawValue))
        {
            return null;
        }

        if (dataType == CellValues.SharedString
            && int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out var sharedStringIndex))
        {
            var value = sharedStrings?.Elements<SharedStringItem>().ElementAtOrDefault(sharedStringIndex)?.InnerText
                ?? rawValue;
            return new WorksheetCell(value, value);
        }

        if (dataType == CellValues.Boolean)
        {
            var value = rawValue == "1" || bool.TryParse(rawValue, out var boolean) && boolean;
            return new WorksheetCell(rawValue, value);
        }

        if (dataType == CellValues.Date
            && DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
        {
            return new WorksheetCell(rawValue, date);
        }

        if ((dataType is null || dataType == CellValues.Number)
            && decimal.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            if (IsDateCell(cell, stylesheet)
                && double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var serialDate))
            {
                var offset = uses1904Dates ? 1462d : 0d;
                return new WorksheetCell(rawValue, DateTime.FromOADate(serialDate + offset));
            }

            if (number == decimal.Truncate(number) && number >= long.MinValue && number <= long.MaxValue)
            {
                return new WorksheetCell(rawValue, decimal.ToInt64(number));
            }

            return new WorksheetCell(rawValue, number);
        }

        return new WorksheetCell(rawValue, rawValue);
    }

    private static bool IsDateCell(Cell cell, Stylesheet? stylesheet)
    {
        if (stylesheet?.CellFormats is null || cell.StyleIndex?.Value is not { } styleIndex)
        {
            return false;
        }

        var format = stylesheet.CellFormats.Elements<CellFormat>().ElementAtOrDefault((int)styleIndex);
        var formatId = format?.NumberFormatId?.Value;
        if (formatId is null)
        {
            return false;
        }

        if (BuiltInDateFormatIds.Contains(formatId.Value))
        {
            return true;
        }

        var formatCode = stylesheet.NumberingFormats?
            .Elements<NumberingFormat>()
            .FirstOrDefault(numberFormat => numberFormat.NumberFormatId?.Value == formatId.Value)
            ?.FormatCode?.Value;
        return formatCode is not null && DateFormatTokenRegex().IsMatch(RemoveQuotedTextRegex().Replace(formatCode, string.Empty));
    }

    private static int GetColumnIndex(string cellReference)
    {
        var index = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            index = checked(index * 26 + char.ToUpperInvariant(character) - 'A' + 1);
        }

        return index - 1;
    }

    private static List<string> CreateUniqueHeaders(string?[] values)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headers = new List<string>(values.Length);
        for (var index = 0; index < values.Length; index++)
        {
            var baseName = string.IsNullOrWhiteSpace(values[index])
                ? $"Column {index + 1}"
                : values[index]!.Trim();
            counts.TryGetValue(baseName, out var count);
            count++;
            counts[baseName] = count;
            headers.Add(count == 1 ? baseName : $"{baseName} ({count})");
        }

        return headers;
    }

    private static DataType InferType(IEnumerable<object?> sourceValues)
    {
        var values = sourceValues.Where(value => value is not null).ToArray();
        if (values.Length == 0)
        {
            return DataType.Unknown;
        }

        if (values.All(value => value is long))
        {
            return DataType.Integer;
        }

        if (values.All(value => value is long or decimal))
        {
            return DataType.Decimal;
        }

        if (values.All(value => value is bool))
        {
            return DataType.Boolean;
        }

        if (values.All(value => value is DateTime))
        {
            return DataType.Date;
        }

        return DataType.Text;
    }

    private static ImportedRow CreateImportedRow(WorksheetRow row, IReadOnlyList<ImportedColumn> columns)
    {
        var cells = columns.Select(column =>
        {
            if (!row.Cells.TryGetValue(column.Index, out var value))
            {
                return new DataCell(column.Id, null);
            }

            var parsedValue = column.DataType == DataType.Text
                ? value.DisplayValue
                : value.ParsedValue;
            return new DataCell(column.Id, value.DisplayValue, parsedValue);
        });
        return new ImportedRow(row.RowNumber, cells);
    }

    private static void ValidateFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The selected XLSX file does not exist.", filePath);
        }
    }

    [GeneratedRegex("[ymdhs]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DateFormatTokenRegex();

    [GeneratedRegex("\"[^\"]*\"|\\\\.", RegexOptions.CultureInvariant)]
    private static partial Regex RemoveQuotedTextRegex();

    private sealed record WorksheetCell(string DisplayValue, object ParsedValue);

    private sealed record WorksheetRow(long RowNumber, Dictionary<int, WorksheetCell> Cells);
}
