using DataCleaner.Application.Abstractions;
using DataCleaner.Domain.Data;
using DataCleaner.Infrastructure.Files;
using System.Text;

namespace DataCleaner.Infrastructure.Tests;

public sealed class CsvDataFileReaderTests
{
    private readonly CsvDataFileReader _reader = new();

    [Fact]
    public void CanRead_AcceptsCsvExtensionIgnoringCase()
    {
        Assert.True(_reader.CanRead(".csv"));
        Assert.True(_reader.CanRead(".CSV"));
        Assert.False(_reader.CanRead(".xlsx"));
    }

    [Fact]
    public async Task ReadAsync_ImportsQuotedFieldsAndInfersTypes()
    {
        const string content = "Name,Age,Active,Balance\r\n\"Smith, Jane\",42,true,1234.50\r\nJohn,31,false,99.25\r\n";

        var dataset = await ReadAsync(content, "en-US");

        Assert.Equal([DataType.Text, DataType.Integer, DataType.Boolean, DataType.Decimal],
            dataset.Columns.Select(column => column.DataType));
        Assert.Equal("Smith, Jane", dataset.Rows[0].Cells[0].CurrentValue);
        Assert.Equal(42L, dataset.Rows[0].Cells[1].CurrentValue);
        Assert.Equal(true, dataset.Rows[0].Cells[2].CurrentValue);
        Assert.Equal(1234.50m, dataset.Rows[0].Cells[3].CurrentValue);
        Assert.Equal(2, dataset.Rows.Count);
    }

    [Fact]
    public async Task ReadAsync_DetectsSemicolonAndUsesRequestedCulture()
    {
        const string content = "Product;Price;Date\r\nCoffee;12,50;14.08.2026\r\nTea;8,25;15.08.2026\r\n";

        var dataset = await ReadAsync(content, "pl-PL");

        Assert.Equal(DataType.Decimal, dataset.Columns[1].DataType);
        Assert.Equal(DataType.Date, dataset.Columns[2].DataType);
        Assert.Equal(12.50m, dataset.Rows[0].Cells[1].CurrentValue);
        Assert.Equal(new DateTime(2026, 8, 14), dataset.Rows[0].Cells[2].CurrentValue);
    }

    [Fact]
    public async Task ReadAsync_MakesDuplicateAndBlankHeadersSafeForPreview()
    {
        const string content = "Name,name,\r\nA,B,C\r\n";

        var dataset = await ReadAsync(content, "en-US");

        Assert.Equal(["Name", "name (2)", "Column 3"],
            dataset.Columns.Select(column => column.SourceName));
    }

    [Fact]
    public async Task ReadAsync_RejectsRowsWiderThanHeader()
    {
        const string content = "Name,Age\r\nJane,42,Unexpected\r\n";

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => ReadAsync(content, "en-US"));

        Assert.Contains("row 2", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAsync_UsesSelectedWindows1250Encoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var directory = Path.Combine(Path.GetTempPath(), "DataCleaner.Tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(directory, "polish.csv");
        Directory.CreateDirectory(directory);

        try
        {
            await File.WriteAllTextAsync(filePath, "City\r\nŁódź\r\n", Encoding.GetEncoding(1250));

            var dataset = await _reader.ReadAsync(new ImportRequest(filePath, null, "pl-PL", "Windows-1250"));

            Assert.Equal("Łódź", dataset.Rows[0].Cells[0].CurrentValue);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_ReportsEncodingMismatch()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var directory = Path.Combine(Path.GetTempPath(), "DataCleaner.Tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(directory, "polish.csv");
        Directory.CreateDirectory(directory);

        try
        {
            await File.WriteAllTextAsync(filePath, "City\r\nŁódź\r\n", Encoding.GetEncoding(1250));

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _reader.ReadAsync(new ImportRequest(filePath, null, "pl-PL", "UTF-8")));

            Assert.Contains("encoding", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private async Task<ImportedDataset> ReadAsync(string content, string cultureName)
    {
        var directory = Path.Combine(Path.GetTempPath(), "DataCleaner.Tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(directory, "sample.csv");
        Directory.CreateDirectory(directory);

        try
        {
            await File.WriteAllTextAsync(filePath, content);
            return await _reader.ReadAsync(new ImportRequest(filePath, null, cultureName));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
