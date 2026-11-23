using PlanejadorCompras.Domain.Rules;
using PlanejadorCompras.Domain.Validation;

namespace PlanejadorCompras.Domain.Entities;

public sealed class QuotationRequest
{
    private readonly List<QuotationRequestItem> _items = [];
    private readonly List<SupplierQuotation> _supplierQuotations = [];

    private QuotationRequest()
    {
    }

    private QuotationRequest(
        Guid id,
        Guid userId,
        Guid sourceShoppingListId,
        string code,
        string shoppingListName,
        string? description,
        string buyerName,
        string buyerEmail,
        DateOnly? responseDeadline,
        string? deliveryAddress,
        string? instructions,
        DateTime createdAtUtc)
    {
        Id = id;
        UserId = userId;
        SourceShoppingListId = sourceShoppingListId;
        Code = code;
        ShoppingListName = DomainText.Required(
            shoppingListName,
            QuotationRequestRules.ShoppingListNameMaxLength,
            nameof(shoppingListName));
        Description = DomainText.Optional(
            description,
            QuotationRequestRules.DescriptionMaxLength,
            nameof(description));
        BuyerName = DomainText.Required(
            buyerName,
            QuotationRequestRules.BuyerNameMaxLength,
            nameof(buyerName));
        BuyerEmail = DomainText.Required(
            buyerEmail,
            QuotationRequestRules.BuyerEmailMaxLength,
            nameof(buyerEmail));
        ResponseDeadline = responseDeadline;
        DeliveryAddress = DomainText.Optional(
            deliveryAddress,
            QuotationRequestRules.DeliveryAddressMaxLength,
            nameof(deliveryAddress));
        Instructions = DomainText.Optional(
            instructions,
            QuotationRequestRules.InstructionsMaxLength,
            nameof(instructions));
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? SourceShoppingListId { get; private set; }
    public string Code { get; private set; } = null!;
    public string ShoppingListName { get; private set; } = null!;
    public string? Description { get; private set; }
    public string BuyerName { get; private set; } = null!;
    public string BuyerEmail { get; private set; } = null!;
    public DateOnly? ResponseDeadline { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public string? Instructions { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<QuotationRequestItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<SupplierQuotation> SupplierQuotations =>
        _supplierQuotations.AsReadOnly();

    /// <summary>
    /// Last instant a supplier can send or change an answer: the end of the deadline day in
    /// Brasília time. Null when the request has no deadline (the link lifetime applies).
    /// </summary>
    public DateTime? ResponseCutoffUtc =>
        ResponseDeadline is { } deadline
            ? deadline.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                - SupplierQuotationRules.DeadlineUtcOffset
            : null;

    public bool AcceptsResponsesAt(DateTime utcNow) =>
        ResponseCutoffUtc is not { } cutoff || utcNow < cutoff;

    /// <summary>Expiration of a public link issued at <paramref name="utcNow"/>.</summary>
    public DateTime CalculateLinkExpiration(DateTime utcNow)
    {
        var expiration = ResponseCutoffUtc is { } cutoff
            ? cutoff + SupplierQuotationRules.ReadOnlyPeriodAfterDeadline
            : utcNow + SupplierQuotationRules.LinkLifetimeWithoutDeadline;
        var minimum = utcNow + SupplierQuotationRules.ReadOnlyPeriodAfterDeadline;

        return expiration > minimum ? expiration : minimum;
    }

    public SupplierQuotation AddSupplierQuotation(
        SupplierQuotationRecipient recipient,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        if (_supplierQuotations.Count >= SupplierQuotationRules.MaximumSuppliersPerRequest)
        {
            throw new InvalidOperationException("The quotation request has too many suppliers.");
        }

        if (_supplierQuotations.Any(quotation => quotation.SupplierId == recipient.SupplierId))
        {
            throw new InvalidOperationException("The supplier is already part of this request.");
        }

        var quotation = SupplierQuotation.Create(
            Id,
            recipient,
            CalculateLinkExpiration(utcNow),
            utcNow);
        _supplierQuotations.Add(quotation);
        return quotation;
    }

    public static QuotationRequest Create(
        Guid userId,
        Guid sourceShoppingListId,
        string shoppingListName,
        string? description,
        string buyerName,
        string buyerEmail,
        DateOnly? responseDeadline,
        string? deliveryAddress,
        string? instructions,
        IReadOnlyCollection<QuotationRequestItemSnapshot> items,
        DateTime createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(sourceShoppingListId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(shoppingListName);
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(buyerEmail);
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) throw new ArgumentException("A quotation request must contain items.", nameof(items));

        var id = Guid.NewGuid();
        var code = $"SC-{createdAtUtc.Year}-{id:N}"[..14].ToUpperInvariant();
        var request = new QuotationRequest(
            id,
            userId,
            sourceShoppingListId,
            code,
            shoppingListName,
            description,
            buyerName,
            buyerEmail.Trim().ToLowerInvariant(),
            responseDeadline,
            deliveryAddress,
            instructions,
            createdAtUtc);

        var position = 0;
        foreach (var item in items)
        {
            request._items.Add(QuotationRequestItem.Create(request.Id, position, item));
            position++;
        }

        return request;
    }
}

public sealed record QuotationRequestItemSnapshot(
    Guid SourceShoppingItemId,
    string Name,
    decimal Quantity,
    string Unit);
