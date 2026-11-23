using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Domain.Entities;

/// <summary>Unit price a supplier informed for one item of the quotation request.</summary>
public sealed class SupplierQuotationResponseItem
{
    private SupplierQuotationResponseItem()
    {
    }

    private SupplierQuotationResponseItem(
        Guid id,
        Guid supplierQuotationId,
        Guid quotationRequestItemId,
        decimal unitPrice)
    {
        Id = id;
        SupplierQuotationId = supplierQuotationId;
        QuotationRequestItemId = quotationRequestItemId;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; }
    public Guid SupplierQuotationId { get; private set; }
    public Guid QuotationRequestItemId { get; private set; }
    public decimal UnitPrice { get; private set; }

    internal static SupplierQuotationResponseItem Create(
        Guid supplierQuotationId,
        Guid quotationRequestItemId,
        decimal unitPrice)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(supplierQuotationId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(quotationRequestItemId, Guid.Empty);
        if (unitPrice < SupplierQuotationRules.MinimumUnitPrice
            || unitPrice > SupplierQuotationRules.MaximumUnitPrice
            || decimal.Round(unitPrice, SupplierQuotationRules.UnitPriceScale) != unitPrice)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        }

        return new SupplierQuotationResponseItem(
            Guid.NewGuid(),
            supplierQuotationId,
            quotationRequestItemId,
            unitPrice);
    }
}
