using PlanejadorCompras.Domain.Rules;
using PlanejadorCompras.Domain.Validation;

namespace PlanejadorCompras.Domain.Entities;

/// <summary>
/// The quotation one supplier receives inside a quotation request. It owns the public link
/// (only the token hash is stored), the tracking dates and the answer given by the supplier.
/// </summary>
public sealed class SupplierQuotation
{
    private readonly List<SupplierQuotationResponseItem> _responseItems = [];

    private SupplierQuotation()
    {
    }

    public Guid Id { get; private set; }
    public Guid QuotationRequestId { get; private set; }
    public Guid SupplierId { get; private set; }
    public string SupplierName { get; private set; } = null!;
    public string? SupplierEmail { get; private set; }
    public string? SupplierPhone { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime TokenExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? EmailSentAtUtc { get; private set; }
    public DateTime? ViewedAtUtc { get; private set; }
    public DateTime? RespondedAtUtc { get; private set; }
    public CommercialTerms Terms { get; private set; } = CommercialTerms.Empty();
    public IReadOnlyCollection<SupplierQuotationResponseItem> ResponseItems =>
        _responseItems.AsReadOnly();

    public SupplierQuotationStatus Status =>
        RespondedAtUtc.HasValue
            ? SupplierQuotationStatus.Responded
            : ViewedAtUtc.HasValue
                ? SupplierQuotationStatus.Viewed
                : SupplierQuotationStatus.Sent;

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsExpiredAt(DateTime utcNow) => utcNow > TokenExpiresAtUtc;

    /// <summary>True while the public link can be opened.</summary>
    public bool IsLinkActiveAt(DateTime utcNow) => !IsRevoked && !IsExpiredAt(utcNow);

    internal static SupplierQuotation Create(
        Guid quotationRequestId,
        SupplierQuotationRecipient recipient,
        DateTime tokenExpiresAtUtc,
        DateTime createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(quotationRequestId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentOutOfRangeException.ThrowIfEqual(recipient.SupplierId, Guid.Empty);
        EnsureValidToken(recipient.TokenHash, tokenExpiresAtUtc, createdAtUtc);

        return new SupplierQuotation
        {
            Id = Guid.NewGuid(),
            QuotationRequestId = quotationRequestId,
            SupplierId = recipient.SupplierId,
            SupplierName = DomainText.Required(
                recipient.Name,
                SupplierQuotationRules.SupplierNameMaxLength,
                nameof(recipient)),
            SupplierEmail = DomainText.Optional(
                recipient.Email,
                SupplierQuotationRules.SupplierEmailMaxLength,
                nameof(recipient)),
            SupplierPhone = DomainText.Optional(
                recipient.Phone,
                SupplierQuotationRules.SupplierPhoneMaxLength,
                nameof(recipient)),
            TokenHash = recipient.TokenHash,
            TokenExpiresAtUtc = tokenExpiresAtUtc,
            CreatedAtUtc = createdAtUtc
        };
    }

    /// <summary>Replaces the link: the previous token stops working immediately.</summary>
    public void RotateToken(string tokenHash, DateTime tokenExpiresAtUtc, DateTime utcNow)
    {
        EnsureValidToken(tokenHash, tokenExpiresAtUtc, utcNow);
        TokenHash = tokenHash;
        TokenExpiresAtUtc = tokenExpiresAtUtc;
        RevokedAtUtc = null;
    }

    public void UpdateContact(string? supplierEmail, string? supplierPhone)
    {
        SupplierEmail = DomainText.Optional(
            supplierEmail,
            SupplierQuotationRules.SupplierEmailMaxLength,
            nameof(supplierEmail));
        SupplierPhone = DomainText.Optional(
            supplierPhone,
            SupplierQuotationRules.SupplierPhoneMaxLength,
            nameof(supplierPhone));
    }

    public void Revoke(DateTime utcNow)
    {
        RevokedAtUtc ??= utcNow;
    }

    public void MarkEmailSent(DateTime utcNow)
    {
        EmailSentAtUtc = utcNow;
    }

    public void MarkViewed(DateTime utcNow)
    {
        ViewedAtUtc ??= utcNow;
    }

    /// <summary>
    /// Records (or replaces) the answer. The caller is responsible for checking the link
    /// and the response deadline before calling.
    /// </summary>
    public void Respond(
        IReadOnlyCollection<SupplierQuotationPrice> prices,
        CommercialTerms terms,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(prices);
        ArgumentNullException.ThrowIfNull(terms);
        if (prices.Count == 0)
        {
            throw new ArgumentException("A response must contain at least one price.", nameof(prices));
        }

        if (prices.Count > SupplierQuotationRules.MaximumPricesPerResponse
            || prices.Select(price => price.QuotationRequestItemId).Distinct().Count() != prices.Count)
        {
            throw new ArgumentException("Response prices are invalid.", nameof(prices));
        }

        _responseItems.Clear();
        foreach (var price in prices)
        {
            _responseItems.Add(SupplierQuotationResponseItem.Create(
                Id,
                price.QuotationRequestItemId,
                price.UnitPrice));
        }

        Terms = terms;
        ViewedAtUtc ??= utcNow;
        RespondedAtUtc = utcNow;
    }

    private static void EnsureValidToken(
        string tokenHash,
        DateTime tokenExpiresAtUtc,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        if (tokenHash.Length != SupplierQuotationRules.TokenHashLength)
        {
            throw new ArgumentException("Token hash has an invalid length.", nameof(tokenHash));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tokenExpiresAtUtc, utcNow);
    }
}

public sealed record SupplierQuotationPrice(Guid QuotationRequestItemId, decimal UnitPrice);

public sealed record SupplierQuotationRecipient(
    Guid SupplierId,
    string Name,
    string? Email,
    string? Phone,
    string TokenHash);
