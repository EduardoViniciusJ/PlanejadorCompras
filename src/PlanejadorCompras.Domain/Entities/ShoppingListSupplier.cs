namespace PlanejadorCompras.Domain.Entities;

public sealed class ShoppingListSupplier
{
    private ShoppingListSupplier(Guid shoppingListId, Guid supplierId, DateTime createdAt)
    {
        ShoppingListId = shoppingListId;
        SupplierId = supplierId;
        CreatedAt = createdAt;
    }

    public Guid ShoppingListId { get; private set; }

    public Guid SupplierId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>Conditions offered by this supplier for this list (typed by the buyer or sent by the supplier).</summary>
    public CommercialTerms Terms { get; private set; } = CommercialTerms.Empty();

    public DateTime? TermsUpdatedAtUtc { get; private set; }

    public static ShoppingListSupplier Create(Guid shoppingListId, Guid supplierId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(shoppingListId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(supplierId, Guid.Empty);

        return new ShoppingListSupplier(shoppingListId, supplierId, DateTime.UtcNow);
    }

    public void UpdateTerms(CommercialTerms terms, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(terms);
        Terms = terms;
        TermsUpdatedAtUtc = utcNow;
    }
}
