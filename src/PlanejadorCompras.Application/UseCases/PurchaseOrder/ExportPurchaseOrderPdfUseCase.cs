using PlanejadorCompras.Application.Features.Reports.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Application.UseCases.PurchaseOrder;

public sealed class ExportPurchaseOrderPdfUseCase(
    IPurchaseOrderAccessService purchaseOrderAccessService,
    IPurchaseOrderPdfExporter pdfExporter)
{
    public async Task<ExportedFileDto> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await purchaseOrderAccessService.GetForCurrentUserAsync(
            id,
            cancellationToken);

        return await pdfExporter.ExportAsync(
            PurchaseOrderResponseMapper.ToReport(order),
            cancellationToken);
    }
}

public sealed class ExportPurchaseOrderExcelUseCase(
    IPurchaseOrderAccessService purchaseOrderAccessService,
    IPurchaseOrderExcelExporter excelExporter)
{
    public async Task<ExportedFileDto> ExecuteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await purchaseOrderAccessService.GetForCurrentUserAsync(
            id,
            cancellationToken);

        return await excelExporter.ExportAsync(
            PurchaseOrderResponseMapper.ToReport(order),
            cancellationToken);
    }
}
