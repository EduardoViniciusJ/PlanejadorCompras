using ClosedXML.Excel;
using PlanejadorCompras.Application.Features.Reports.Contracts;
using PlanejadorCompras.Infrastructure.Reports.Excel;

namespace PlanejadorCompras.Application.UnitTests.Reports.Excel;

public sealed class ClosedXmlPurchaseOrderExporterTests
{
    [Fact]
    public async Task ExportAsync_ShouldCreateWorkbookWithTypedItemsAndTotal()
    {
        var file = await new ClosedXmlPurchaseOrderExporter().ExportAsync(CreateReport());

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            file.ContentType);
        Assert.Equal("pedido-compra-pc-2026-abc12345.xlsx", file.FileName);

        using var workbook = Open(file);
        var worksheet = workbook.Worksheet("Pedido de compra");
        Assert.Equal("Pedido de compra PC-2026-ABC12345", worksheet.Cell("A1").GetString());
        var header = worksheet.CellsUsed(cell => cell.Value.IsText && cell.GetString() == "Preço unitário").Single();
        var firstItemRow = header.Address.RowNumber + 1;
        Assert.Equal("Mouse", worksheet.Cell(firstItemRow, 1).GetString());
        Assert.Equal(XLDataType.Number, worksheet.Cell(firstItemRow, 2).DataType);
        Assert.Equal(1m, worksheet.Cell(firstItemRow, 2).GetValue<decimal>());
        Assert.Equal(0.01m, worksheet.Cell(firstItemRow, 4).GetValue<decimal>());
        Assert.Equal(2634m, worksheet.Cell(firstItemRow + 1, 5).GetValue<decimal>());
        Assert.Equal("Total do pedido", worksheet.Cell(firstItemRow + 2, 1).GetString());
        Assert.Equal(2634.01m, worksheet.Cell(firstItemRow + 2, 5).GetValue<decimal>());
        Assert.Equal(XLDataType.DateTime, worksheet.Cell("B9").DataType);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://evil.test\",\"clique\")")]
    [InlineData("+1+1")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1:A2)")]
    [InlineData("=cmd|' /C calc'!A0")]
    public async Task ExportAsync_ShouldPersistUserTextWithoutFormulas(string unsafeText)
    {
        var report = CreateReport() with
        {
            SupplierName = unsafeText,
            ShoppingListName = unsafeText,
            BuyerName = unsafeText,
            DeliveryAddress = unsafeText,
            PaymentTerms = unsafeText,
            Notes = unsafeText,
            Items = [new PurchaseOrderReportItemDto(unsafeText, 2, unsafeText, 10m, 20m)]
        };

        var file = await new ClosedXmlPurchaseOrderExporter().ExportAsync(report);

        using var workbook = Open(file);
        var cells = workbook.Worksheet(1).CellsUsed().ToList();
        Assert.All(cells, cell => Assert.False(cell.HasFormula));
        var unsafeCells = cells
            .Where(cell => cell.Value.IsText && cell.GetString() == unsafeText)
            .ToList();
        // supplier, list, buyer, delivery address, payment terms, notes, item name and unit
        Assert.Equal(8, unsafeCells.Count);
        Assert.All(unsafeCells, cell => Assert.Equal(XLDataType.Text, cell.DataType));
    }

    [Fact]
    public async Task ExportAsync_ShouldHandleMissingOptionalFields()
    {
        var report = CreateReport() with
        {
            BuyerEmail = null,
            ExpectedDeliveryDate = null,
            DeliveryAddress = null,
            PaymentTerms = null,
            Notes = null
        };

        var file = await new ClosedXmlPurchaseOrderExporter().ExportAsync(report);

        using var workbook = Open(file);
        Assert.Equal("Não informada", workbook.Worksheet(1).Cell("B9").GetString());
    }

    [Fact]
    public async Task ExportAsync_ShouldHonorCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ClosedXmlPurchaseOrderExporter().ExportAsync(CreateReport(), cancellation.Token));
    }

    private static XLWorkbook Open(ExportedFileDto file) => new(new MemoryStream(file.Content));

    private static PurchaseOrderReportDataDto CreateReport() =>
        new(
            "PC-2026-ABC12345",
            "Compras para escritório",
            "Fornecedor A",
            "Marina Lopes",
            "marina@example.com",
            new DateOnly(2026, 8, 15),
            "Rua das Compras, 100",
            "30 dias",
            "Entregar no almoxarifado.",
            "Emitido",
            new DateTime(2026, 7, 30, 15, 0, 0, DateTimeKind.Utc),
            2634.01m,
            [
                new PurchaseOrderReportItemDto("Mouse", 1, "un", 0.01m, 0.01m),
                new PurchaseOrderReportItemDto("Teclado", 3, "un", 878m, 2634m)
            ]);
}
