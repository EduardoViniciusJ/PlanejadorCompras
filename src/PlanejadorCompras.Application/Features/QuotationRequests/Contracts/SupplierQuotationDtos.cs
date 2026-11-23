using System.ComponentModel.DataAnnotations;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Application.Features.QuotationRequests.Contracts;

public sealed record CommercialTermsDto(
    int? DeliveryDays,
    string? PaymentTerms,
    decimal? FreightCost,
    decimal? MinimumOrderValue,
    DateOnly? ProposalValidUntil,
    string? Notes);

public sealed record CommercialTermsRequestDto(
    [Range(CommercialTermsRules.MinimumDeliveryDays, CommercialTermsRules.MaximumDeliveryDays)]
    int? DeliveryDays = null,
    [MaxLength(CommercialTermsRules.PaymentTermsMaxLength)]
    string? PaymentTerms = null,
    [Range(
        typeof(decimal),
        CommercialTermsRules.MinimumAmountText,
        CommercialTermsRules.MaximumAmountText,
        ParseLimitsInInvariantCulture = true)]
    decimal? FreightCost = null,
    [Range(
        typeof(decimal),
        CommercialTermsRules.MinimumAmountText,
        CommercialTermsRules.MaximumAmountText,
        ParseLimitsInInvariantCulture = true)]
    decimal? MinimumOrderValue = null,
    DateOnly? ProposalValidUntil = null,
    [MaxLength(CommercialTermsRules.NotesMaxLength)]
    string? Notes = null);

/// <summary>Status values: "sent", "viewed" or "responded".</summary>
public sealed record SupplierQuotationSummaryDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string Status,
    DateTime SentAtUtc,
    DateTime? ViewedAtUtc,
    DateTime? RespondedAtUtc,
    bool IsLinkActive);

public sealed record SupplierQuotationPriceDto(Guid ItemId, decimal UnitPrice);

/// <summary>
/// <see cref="LinkUrl"/> is only filled right after the link is created or replaced:
/// the server keeps just the hash of the token and cannot rebuild the address later.
/// </summary>
public sealed record SupplierQuotationResponseDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string? SupplierEmail,
    string? SupplierPhone,
    string Status,
    DateTime SentAtUtc,
    DateTime? EmailSentAtUtc,
    DateTime? ViewedAtUtc,
    DateTime? RespondedAtUtc,
    DateTime LinkExpiresAtUtc,
    bool IsLinkRevoked,
    bool IsLinkActive,
    CommercialTermsDto? Terms,
    IReadOnlyCollection<SupplierQuotationPriceDto> Prices,
    string? LinkUrl = null,
    string? EmailDelivery = null);

public sealed record SupplierQuotationLinkRequestDto(bool SendEmail = false);

public sealed record QuotationDeliveryOptionsDto(bool EmailAvailable);

public sealed record PublicQuotationItemDto(
    Guid Id,
    string Name,
    decimal Quantity,
    string Unit,
    decimal? UnitPrice);

/// <summary>
/// Everything a supplier sees through the public link. It intentionally has no buyer
/// e-mail, no internal ids of the list and nothing about other suppliers.
/// </summary>
public sealed record PublicSupplierQuotationResponseDto(
    string Code,
    string BuyerName,
    string SupplierName,
    string Subject,
    DateOnly? ResponseDeadline,
    string? DeliveryAddress,
    string? Instructions,
    bool CanRespond,
    DateTime? RespondedAtUtc,
    CommercialTermsDto? Terms,
    IReadOnlyCollection<PublicQuotationItemDto> Items);

public sealed record SubmitSupplierQuotationPriceDto(
    [Required]
    Guid ItemId,
    [Required]
    [Range(
        typeof(decimal),
        SupplierQuotationRules.MinimumUnitPriceText,
        SupplierQuotationRules.MaximumUnitPriceText,
        ParseLimitsInInvariantCulture = true)]
    decimal UnitPrice);

public sealed record SubmitSupplierQuotationRequestDto(
    [Required]
    [MinLength(1)]
    [MaxLength(SupplierQuotationRules.MaximumPricesPerResponse)]
    IReadOnlyList<SubmitSupplierQuotationPriceDto> Prices,
    [Range(CommercialTermsRules.MinimumDeliveryDays, CommercialTermsRules.MaximumDeliveryDays)]
    int? DeliveryDays = null,
    [MaxLength(CommercialTermsRules.PaymentTermsMaxLength)]
    string? PaymentTerms = null,
    [Range(
        typeof(decimal),
        CommercialTermsRules.MinimumAmountText,
        CommercialTermsRules.MaximumAmountText,
        ParseLimitsInInvariantCulture = true)]
    decimal? FreightCost = null,
    [Range(
        typeof(decimal),
        CommercialTermsRules.MinimumAmountText,
        CommercialTermsRules.MaximumAmountText,
        ParseLimitsInInvariantCulture = true)]
    decimal? MinimumOrderValue = null,
    DateOnly? ProposalValidUntil = null,
    [MaxLength(CommercialTermsRules.NotesMaxLength)]
    string? Notes = null);

public sealed record ShoppingListSupplierTermsResponseDto(
    Guid SupplierId,
    CommercialTermsDto? Terms,
    DateTime? UpdatedAtUtc);
