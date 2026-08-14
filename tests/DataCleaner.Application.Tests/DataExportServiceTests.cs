using DataCleaner.Application.Abstractions;
using DataCleaner.Application.Export;
using DataCleaner.Domain.Data;
using System.Globalization;

namespace DataCleaner.Application.Tests;

public sealed class DataExportServiceTests
{
    [Theory]
    [InlineData(ExportRowFilter.All, 4)]
    [InlineData(ExportRowFilter.Valid, 2)]
    [InlineData(ExportRowFilter.Invalid, 2)]
    [InlineData(ExportRowFilter.Modified, 1)]
    public async Task ExportAsync_FiltersRowsByCoexistingStateFlags(ExportRowFilter filter, int expected)
    {
        var writer = new CapturingWriter();
        var service = new DataExportService([writer]);
        var dataset = CreateDataset();

        var result = await service.ExportAsync(new ExportRequest("output.csv", dataset, filter));

        Assert.Equal(expected, result.ExportedRows);
        Assert.Equal(expected, writer.Dataset!.Rows.Count);
        Assert.Equal(4, dataset.Rows.Count);
    }

    [Fact]
    public async Task ExportAsync_RejectsUnsupportedFormats()
    {
        var service = new DataExportService([]);

        await Assert.ThrowsAsync<NotSupportedException>(() => service.ExportAsync(
            new ExportRequest("output.json", CreateDataset())));
    }

    private static ImportedDataset CreateDataset()
    {
        var column = new ImportedColumn(0, "Value");
        var rows = Enumerable.Range(2, 4)
            .Select(number => new ImportedRow(number, [new DataCell(column.Id, number.ToString(CultureInfo.InvariantCulture), number)]))
            .ToArray();
        rows[1].AddState(RowState.Invalid | RowState.Rejected);
        rows[2].AddState(RowState.Modified);
        rows[3].AddState(RowState.Invalid);
        return new ImportedDataset("source.csv", [column], rows);
    }

    private sealed class CapturingWriter : IDataFileWriter
    {
        public ImportedDataset? Dataset { get; private set; }

        public bool CanWrite(string fileExtension) => fileExtension == ".csv";

        public Task WriteAsync(ExportRequest request, CancellationToken cancellationToken = default)
        {
            Dataset = request.Dataset;
            return Task.CompletedTask;
        }
    }
}
