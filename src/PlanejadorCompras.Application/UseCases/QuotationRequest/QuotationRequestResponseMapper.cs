using PlanejadorCompras.Application.Features.Reports.Contracts;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Domain.Entities;
using QuotationRequestEntity = PlanejadorCompras.Domain.Entities.QuotationRequest;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

internal static class QuotationRequestResponseMapper
{
    public static QuotationRequestSummaryResponseDto ToSummary(
        QuotationRequestEntity request,
        DateTime utcNow) =>
        new(
            request.Id,
            request.Code,
            request.SourceShoppingListId,
            request.ShoppingListName,
            request.BuyerName,
            request.Items.Count,
            request.ResponseDeadline,
            request.CreatedAtUtc,
            OrderSuppliers(request)
                .Select(quotation => new SupplierQuotationSummaryDto(
                    quotation.Id,
                    quotation.SupplierId,
                    quotation.SupplierName,
                    ToStatus(quotation.Status),
                    quotation.CreatedAtUtc,
                    quotation.ViewedAtUtc,
                    quotation.RespondedAtUtc,
                    quotation.IsLinkActiveAt(utcNow)))
                .ToList());

    public static QuotationRequestDetailResponseDto ToDetail(
        QuotationRequestEntity request,
        DateTime utcNow,
        Func<SupplierQuotation, SupplierQuotationResponseDto>? supplierMapper = null) =>
        new(
            request.Id,
            request.Code,
            request.SourceShoppingListId,
            request.ShoppingListName,
            request.Description,
            request.BuyerName,
            request.BuyerEmail,
            request.ResponseDeadline,
            request.DeliveryAddress,
            request.Instructions,
            request.CreatedAtUtc,
            request.Items
                .OrderBy(item => item.Position)
                .Select(item => new QuotationRequestItemResponseDto(
                    item.Id,
                    item.SourceShoppingItemId,
                    item.Name,
                    item.Quantity,
                    item.Unit))
                .ToList(),
            OrderSuppliers(request)
                .Select(supplierMapper ?? (quotation => ToSupplier(quotation, utcNow)))
                .ToList());

    public static SupplierQuotationResponseDto ToSupplier(
        SupplierQuotation quotation,
        DateTime utcNow,
        string? linkUrl = null,
        string? emailDelivery = null) =>
        new(
            quotation.Id,
            quotation.SupplierId,
            quotation.SupplierName,
            quotation.SupplierEmail,
            quotation.SupplierPhone,
            ToStatus(quotation.Status),
            quotation.CreatedAtUtc,
            quotation.EmailSentAtUtc,
            quotation.ViewedAtUtc,
            quotation.RespondedAtUtc,
            quotation.TokenExpiresAtUtc,
            quotation.IsRevoked,
            quotation.IsLinkActiveAt(utcNow),
            ToTerms(quotation.Terms),
            quotation.ResponseItems
                .Select(item => new SupplierQuotationPriceDto(
                    item.QuotationRequestItemId,
                    item.UnitPrice))
                .ToList(),
            linkUrl,
            emailDelivery);

    public static CommercialTermsDto? ToTerms(CommercialTerms terms) =>
        terms.IsEmpty
            ? null
            : new CommercialTermsDto(
                terms.DeliveryDays,
                terms.PaymentTerms,
                terms.FreightCost,
                terms.MinimumOrderValue,
                terms.ProposalValidUntil,
                terms.Notes);

    public static string ToStatus(SupplierQuotationStatus status) =>
        status switch
        {
            SupplierQuotationStatus.Responded => "responded",
            SupplierQuotationStatus.Viewed => "viewed",
            _ => "sent"
        };

    public static QuotationRequestReportDataDto ToReport(
        QuotationRequestEntity request) =>
        new(
            request.SourceShoppingListId ?? Guid.Empty,
            request.Code,
            request.ShoppingListName,
            request.Description,
            request.BuyerName,
            request.BuyerEmail,
            DateOnly.FromDateTime(request.CreatedAtUtc),
            request.ResponseDeadline,
            request.DeliveryAddress,
            request.Instructions,
            request.Items
                .OrderBy(item => item.Position)
                .Select(item => new QuotationRequestReportItemDto(
                    item.Name,
                    item.Quantity,
                    item.Unit))
                .ToList());

    private static IEnumerable<SupplierQuotation> OrderSuppliers(QuotationRequestEntity request) =>
        request.SupplierQuotations
            .OrderBy(quotation => quotation.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(quotation => quotation.Id);
}
