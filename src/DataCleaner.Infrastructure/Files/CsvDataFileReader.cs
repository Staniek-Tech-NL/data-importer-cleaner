using System.Globalization;
using System.Text;
using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Data;
using Microsoft.VisualBasic.FileIO;

namespace DataCleaner.Infrastructure.Files;

public sealed class CsvDataFileReader : IDataFileReader
{
    private static readonly char[] CandidateDelimiters = [',', ';', '\t'];

    static CsvDataFileReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public bool CanRead(string fileExtension) =>
        string.Equals(fileExtension, ".csv", StringComparison.OrdinalIgnoreCase);

    public Task<ImportedDataset> ReadAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(request.FilePath))
        {
            throw new FileNotFoundException("The selected CSV file does not exist.", request.FilePath);
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(request.CultureName);
            var encoding = ResolveEncoding(request.EncodingName);
            var delimiter = DetectDelimiter(request.FilePath, encoding);
            var rawRows = ReadRows(request.FilePath, delimiter, encoding, cancellationToken);
            if (rawRows.Count == 0)
            {
                throw new InvalidDataException("The CSV file is empty.");
            }

            var headerIndex = rawRows.FindIndex(row => row.Any(value => !string.IsNullOrWhiteSpace(value)));
            if (headerIndex < 0)
            {
                throw new InvalidDataException("The CSV file does not contain a header row.");
            }

            var headers = CreateUniqueHeaders(rawRows[headerIndex]);
            if (headers.Count == 0)
            {
                throw new InvalidDataException("The CSV file does not contain a header row.");
            }

            var dataRows = rawRows
                .Skip(headerIndex + 1)
                .Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
                .ToArray();
            ValidateRowWidths(dataRows, headers.Count);

            var types = Enumerable.Range(0, headers.Count)
                .Select(index => InferType(dataRows.Select(row => ValueAt(row, index)), culture))
                .ToArray();
            var columns = headers
                .Select((header, index) => new ImportedColumn(Guid.NewGuid(), index, header, types[index]))
                .ToArray();
            var rows = dataRows
                .Select((values, index) => CreateRow(index + 2L, values, columns, culture))
                .ToArray();

            return Task.FromResult(new ImportedDataset(Path.GetFileName(request.FilePath), columns, rows));
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException(
                "The CSV file cannot be decoded with the selected encoding.",
                exception);
        }
    }

    private static char DetectDelimiter(string filePath, Encoding encoding)
    {
        using var reader = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks: true);
        var firstLine = reader.ReadLine();
        if (string.IsNullOrEmpty(firstLine))
        {
            return ',';
        }

        return CandidateDelimiters
            .Select(delimiter => new { Delimiter = delimiter, Count = CountOutsideQuotes(firstLine, delimiter) })
            .OrderByDescending(candidate => candidate.Count)
            .First()
            .Delimiter;
    }

    private static int CountOutsideQuotes(string value, char delimiter)
    {
        var count = 0;
        var insideQuotes = false;
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '"')
            {
                if (insideQuotes && index + 1 < value.Length && value[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                insideQuotes = !insideQuotes;
            }
            else if (!insideQuotes && value[index] == delimiter)
            {
                count++;
            }
        }

        return count;
    }

    private static List<string[]> ReadRows(
        string filePath,
        char delimiter,
        Encoding encoding,
        CancellationToken cancellationToken)
    {
        using var parser = new TextFieldParser(filePath, encoding, detectEncoding: true)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(delimiter.ToString());

        var rows = new List<string[]>();
        while (!parser.EndOfData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                rows.Add(parser.ReadFields() ?? []);
            }
            catch (MalformedLineException exception)
            {
                throw new InvalidDataException(
                    $"CSV row {parser.ErrorLineNumber} has invalid quoting.",
                    exception);
            }
        }

        return rows;
    }

    private static Encoding ResolveEncoding(string? encodingName)
    {
        if (string.IsNullOrWhiteSpace(encodingName)
            || string.Equals(encodingName, "UTF-8", StringComparison.OrdinalIgnoreCase))
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        }

        try
        {
            return Encoding.GetEncoding(
                encodingName,
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"Encoding '{encodingName}' is not supported.", exception);
        }
    }

    private static List<string> CreateUniqueHeaders(string[] values)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headers = new List<string>(values.Length);
        for (var index = 0; index < values.Length; index++)
        {
            var baseName = string.IsNullOrWhiteSpace(values[index])
                ? $"Column {index + 1}"
                : values[index].Trim();
            counts.TryGetValue(baseName, out var count);
            count++;
            counts[baseName] = count;
            headers.Add(count == 1 ? baseName : $"{baseName} ({count})");
        }

        return headers;
    }

    private static void ValidateRowWidths(IEnumerable<string[]> rows, int columnCount)
    {
        var rowNumber = 2;
        foreach (var row in rows)
        {
            if (row.Length > columnCount)
            {
                throw new InvalidDataException(
                    $"CSV row {rowNumber} contains {row.Length} fields, but the header contains {columnCount}.");
            }

            rowNumber++;
        }
    }

    private static ImportedRow CreateRow(
        long sourceRowNumber,
        IReadOnlyList<string> values,
        IReadOnlyList<ImportedColumn> columns,
        CultureInfo culture)
    {
        var cells = columns.Select(column =>
        {
            var sourceValue = ValueAt(values, column.Index);
            return new DataCell(column.Id, sourceValue, Parse(sourceValue, column.DataType, culture));
        });
        return new ImportedRow(sourceRowNumber, cells);
    }

    private static string? ValueAt(IReadOnlyList<string> values, int index) =>
        index < values.Count ? values[index] : null;

    private static DataType InferType(IEnumerable<string?> sourceValues, CultureInfo culture)
    {
        var values = sourceValues.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (values.Length == 0)
        {
            return DataType.Unknown;
        }

        if (values.All(value => long.TryParse(value, NumberStyles.Integer, culture, out _)))
        {
            return DataType.Integer;
        }

        if (values.All(value => decimal.TryParse(value, NumberStyles.Number, culture, out _)))
        {
            return DataType.Decimal;
        }

        if (values.All(value => bool.TryParse(value, out _)))
        {
            return DataType.Boolean;
        }

        if (values.All(value => DateTime.TryParse(value, culture, DateTimeStyles.AllowWhiteSpaces, out _)))
        {
            return DataType.Date;
        }

        return DataType.Text;
    }

    private static object? Parse(string? value, DataType type, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return type switch
        {
            DataType.Integer when long.TryParse(value, NumberStyles.Integer, culture, out var integer) => integer,
            DataType.Decimal when decimal.TryParse(value, NumberStyles.Number, culture, out var number) => number,
            DataType.Boolean when bool.TryParse(value, out var boolean) => boolean,
            DataType.Date when DateTime.TryParse(value, culture, DateTimeStyles.AllowWhiteSpaces, out var date) => date,
            _ => value
        };
    }
}
