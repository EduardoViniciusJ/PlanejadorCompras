using ClosedXML.Excel;
using PlanejadorCompras.Application.Features.Reports.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Infrastructure.Reports.Excel;

public sealed class ClosedXmlPurchaseOrderExporter : IPurchaseOrderExcelExporter
{
    private const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const int LastColumn = 5;
    private const string DateFormat = "dd/mm/yyyy";

    public Task<ExportedFileDto> ExportAsync(
        PurchaseOrderReportDataDto reportData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reportData);
        cancellationToken.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Pedido de compra");
        ClosedXmlReportStyles.ApplyWorksheetDefaults(worksheet);

        worksheet.Range(1, 1, 1, LastColumn).Merge();
        // Every text below goes through SetText: values typed by users (names, notes, items)
        // are stored as plain text and never interpreted as formulas.
        ClosedXmlReportStyles.SetText(worksheet.Cell(1, 1), $"Pedido de compra {reportData.Code}");
        ClosedXmlReportStyles.ApplyTitleStyle(worksheet.Range(1, 1, 1, LastColumn));

        var row = 3;
        row = WriteInformation(worksheet, row, "Fornecedor", reportData.SupplierName);
        row = WriteInformation(worksheet, row, "Lista de origem", reportData.ShoppingListName);
        row = WriteInformation(worksheet, row, "Comprador", reportData.BuyerName);
        row = WriteInformation(worksheet, row, "E-mail do comprador", reportData.BuyerEmail);
        row = WriteInformation(worksheet, row, "Situação", reportData.Status);
        ClosedXmlReportStyles.SetText(worksheet.Cell(row, 1), "Emitido em");
        worksheet.Range(row, 2, row, LastColumn).Merge();
        ClosedXmlReportStyles.SetDateTime(worksheet.Cell(row, 2), reportData.CreatedAtUtc);
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        row++;
        ClosedXmlReportStyles.SetText(worksheet.Cell(row, 1), "Entrega prevista");
        worksheet.Range(row, 2, row, LastColumn).Merge();
        if (reportData.ExpectedDeliveryDate is { } deliveryDate)
        {
            worksheet.Cell(row, 2).SetValue(deliveryDate.ToDateTime(TimeOnly.MinValue));
            worksheet.Cell(row, 2).Style.NumberFormat.Format = DateFormat;
            worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        }
        else
        {
            ClosedXmlReportStyles.SetText(worksheet.Cell(row, 2), "Não informada");
        }

        row++;
        row = WriteInformation(worksheet, row, "Local de entrega", reportData.DeliveryAddress, wrap: true);
        row = WriteInformation(worksheet, row, "Condições de pagamento", reportData.PaymentTerms, wrap: true);
        row = WriteInformation(worksheet, row, "Observações", reportData.Notes, wrap: true);
        var informationLastRow = row - 1;
        worksheet.Range(3, 1, informationLastRow, 1).Style.Font.Bold = true;
        ClosedXmlReportStyles.ApplyRangeBorders(worksheet.Range(3, 1, informationLastRow, LastColumn));
        cancellationToken.ThrowIfCancellationRequested();

        var headerRow = row + 1;
        string[] headers = ["Item", "Quantidade", "Unidade", "Preço unitário", "Total"];
        for (var column = 0; column < headers.Length; column++)
        {
            ClosedXmlReportStyles.SetText(worksheet.Cell(headerRow, column + 1), headers[column]);
        }

        ClosedXmlReportStyles.ApplyHeaderStyle(worksheet.Range(headerRow, 1, headerRow, LastColumn));

        row = headerRow + 1;
        foreach (var item in reportData.Items)
        {
            ClosedXmlReportStyles.SetText(worksheet.Cell(row, 1), item.Name);
            worksheet.Cell(row, 1).Style.Alignment.WrapText = true;
            worksheet.Cell(row, 2).SetValue(item.Quantity);
            worksheet.Cell(row, 2).Style.NumberFormat.Format = ClosedXmlReportStyles.QuantityFormat;
            ClosedXmlReportStyles.SetText(worksheet.Cell(row, 3), item.Unit);
            ClosedXmlReportStyles.SetCurrency(worksheet.Cell(row, 4), item.UnitPrice);
            ClosedXmlReportStyles.SetCurrency(worksheet.Cell(row, 5), item.TotalPrice);
            row++;
        }

        var totalRow = row;
        worksheet.Range(totalRow, 1, totalRow, LastColumn - 1).Merge();
        ClosedXmlReportStyles.SetText(worksheet.Cell(totalRow, 1), "Total do pedido");
        worksheet.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ClosedXmlReportStyles.SetCurrency(worksheet.Cell(totalRow, LastColumn), reportData.TotalPrice);
        worksheet.Range(totalRow, 1, totalRow, LastColumn).Style.Font.Bold = true;
        ClosedXmlReportStyles.ApplyRangeBorders(worksheet.Range(headerRow, 1, totalRow, LastColumn));

        worksheet.Column(1).Width = 42;
        worksheet.Column(2).Width = 14;
        worksheet.Column(3).Width = 14;
        worksheet.Columns(4, 5).Width = 18;
        worksheet.SheetView.FreezeRows(1);
        ClosedXmlReportStyles.ConfigurePrintLayout(
            worksheet,
            landscape: false,
            fitToPageWidth: true,
            printAreaLastRow: totalRow,
            printAreaLastColumn: LastColumn,
            repeatedHeaderRow: headerRow);
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return Task.FromResult(new ExportedFileDto(
            stream.ToArray(),
            ContentType,
            ReportFileNameBuilder.BuildPurchaseOrderFileName(reportData.Code, "xlsx")));
    }

    private static int WriteInformation(
        IXLWorksheet worksheet,
        int row,
        string label,
        string? value,
        bool wrap = false)
    {
        ClosedXmlReportStyles.SetText(worksheet.Cell(row, 1), label);
        worksheet.Range(row, 2, row, LastColumn).Merge();
        ClosedXmlReportStyles.SetText(
            worksheet.Cell(row, 2),
            string.IsNullOrWhiteSpace(value) ? "Não informado" : value);
        if (wrap)
        {
            worksheet.Cell(row, 2).Style.Alignment.WrapText = true;
            worksheet.Row(row).AdjustToContents(1, LastColumn, 22, 90);
        }

        return row + 1;
    }
}
