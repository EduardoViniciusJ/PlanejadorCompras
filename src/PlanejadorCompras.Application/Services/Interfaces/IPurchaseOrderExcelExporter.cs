using PlanejadorCompras.Application.Features.Reports.Contracts;

namespace PlanejadorCompras.Application.Services.Interfaces;

public interface IPurchaseOrderExcelExporter
{
    Task<ExportedFileDto> ExportAsync(
        PurchaseOrderReportDataDto reportData,
        CancellationToken cancellationToken = default);
}
