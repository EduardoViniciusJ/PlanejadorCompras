using System.ComponentModel.DataAnnotations;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Application.Features.Ai.Contracts;

/// <summary>What the frontend needs to show "X de Y usos este mês" and to enable the AI buttons.</summary>
public sealed record AiStatusResponseDto(
    bool Enabled,
    int Used,
    int Limit,
    int Remaining,
    DateOnly RenewsOn,
    long MaxFileBytes,
    int MaxInputCharacters);

/// <summary>File uploaded by the user, held in memory only for the duration of the request.</summary>
public sealed record AiUploadedFile(string FileName, byte[] Content);

public static class AiMatchConfidence
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
    public const string None = "none";
}

/// <summary>
/// One price line read from the supplier's answer. Nothing here is saved: the buyer
/// reviews every line and confirms through the quote batch endpoint.
/// </summary>
public sealed record ExtractedQuoteLineDto(
    int LineNumber,
    string SupplierDescription,
    string? SupplierUnit,
    decimal? SupplierUnitPrice,
    Guid? ShoppingItemId,
    decimal UnitPrice,
    string Confidence,
    string? Note);

public sealed record QuoteExtractionResponseDto(
    IReadOnlyList<ExtractedQuoteLineDto> Lines,
    string? GeneralConditions,
    int DiscardedLines,
    AiStatusResponseDto Usage);

public sealed record QuoteBatchLineDto(
    [Required]
    Guid ShoppingItemId,
    [Required]
    [Range(
        typeof(decimal),
        ItemQuoteRules.MinimumUnitPriceText,
        ItemQuoteRules.MaximumUnitPriceText,
        ParseLimitsInInvariantCulture = true)]
    decimal UnitPrice);

public sealed record QuoteBatchRequestDto(
    [Required]
    Guid SupplierId,
    [Required]
    [MinLength(1)]
    [MaxLength(QuoteBatchRequestDto.MaxLines)]
    List<QuoteBatchLineDto> Quotes)
{
    public const int MaxLines = 200;
}

public sealed record QuoteBatchResponseDto(int Created, int Updated);

public sealed record DraftShoppingItemDto(string Name, decimal Quantity, string Unit);

public sealed record ShoppingListDraftResponseDto(
    string SuggestedName,
    IReadOnlyList<DraftShoppingItemDto> Items,
    int DiscardedItems,
    AiStatusResponseDto Usage);
