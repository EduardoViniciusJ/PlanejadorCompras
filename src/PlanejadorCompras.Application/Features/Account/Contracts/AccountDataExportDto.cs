namespace PlanejadorCompras.Application.Features.Account.Contracts;

/// <summary>
/// Portable copy of the data held about a user (LGPD, art. 18). Business data
/// is limited to what the user created; data created by colleagues belongs to
/// the organization and is not included.
/// </summary>
public sealed record AccountDataExportDto(
    DateTime GeneratedAtUtc,
    string Scope,
    AccountExportProfileDto Profile,
    IReadOnlyCollection<AccountExportOrganizationDto> Organizations);

public sealed record AccountExportProfileDto(
    Guid Id,
    string Email,
    string? Name,
    DateTime CreatedAt,
    bool EmailConfirmed,
    bool HasPassword,
    bool HasGoogleLogin,
    DateTime? TermsAcceptedAtUtc,
    string? TermsVersion);

public sealed record AccountExportOrganizationDto(
    Guid Id,
    string Name,
    string Role,
    DateTime JoinedAtUtc,
    IReadOnlyCollection<AccountExportShoppingListDto> ShoppingLists,
    IReadOnlyCollection<AccountExportSupplierDto> Suppliers,
    IReadOnlyCollection<AccountExportQuotationRequestDto> QuotationRequests,
    IReadOnlyCollection<AccountExportPurchaseOrderDto> PurchaseOrders,
    IReadOnlyCollection<AccountExportEqualizationDto> Equalizations);

public sealed record AccountExportShoppingListDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    IReadOnlyCollection<AccountExportShoppingItemDto> Items);

public sealed record AccountExportShoppingItemDto(
    string Name,
    decimal Quantity,
    string Unit,
    IReadOnlyCollection<AccountExportItemQuoteDto> Quotes);

public sealed record AccountExportItemQuoteDto(
    string SupplierName,
    decimal UnitPrice,
    DateTime CreatedAt);

public sealed record AccountExportSupplierDto(
    Guid Id,
    string Name,
    string? Cnpj,
    string? Street,
    string? City,
    string? PostalCode,
    string? Email,
    string? Phone,
    DateTime CreatedAt);

public sealed record AccountExportDocumentItemDto(
    string Name,
    decimal Quantity,
    string Unit,
    decimal? UnitPrice);

public sealed record AccountExportQuotationRequestDto(
    string Code,
    string ShoppingListName,
    string BuyerName,
    string BuyerEmail,
    DateOnly? ResponseDeadline,
    string? DeliveryAddress,
    string? Instructions,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<AccountExportDocumentItemDto> Items);

public sealed record AccountExportPurchaseOrderDto(
    string Code,
    string ShoppingListName,
    string SupplierName,
    string BuyerName,
    string? BuyerEmail,
    string Status,
    DateOnly? ExpectedDeliveryDate,
    string? DeliveryAddress,
    string? PaymentTerms,
    string? Notes,
    decimal TotalPrice,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<AccountExportDocumentItemDto> Items);

public sealed record AccountExportEqualizationDto(
    string Code,
    string ShoppingListName,
    string CreatedByName,
    string CreatedByEmail,
    decimal BestChoiceTotal,
    string? BestCompleteSupplierName,
    decimal? BestCompleteSupplierTotal,
    decimal EstimatedEconomy,
    DateTime CreatedAtUtc);
