using PlanejadorCompras.Domain.Rules;
using PlanejadorCompras.Domain.Validation;

namespace PlanejadorCompras.Domain.Entities;

/// <summary>
/// Commercial conditions offered by a supplier. Every field is optional:
/// a null freight means "not informed", zero means free delivery.
/// </summary>
public sealed class CommercialTerms
{
    private CommercialTerms()
    {
    }

    public int? DeliveryDays { get; private set; }
    public string? PaymentTerms { get; private set; }
    public decimal? FreightCost { get; private set; }
    public decimal? MinimumOrderValue { get; private set; }
    public DateOnly? ProposalValidUntil { get; private set; }
    public string? Notes { get; private set; }

    public bool IsEmpty =>
        DeliveryDays is null
        && PaymentTerms is null
        && FreightCost is null
        && MinimumOrderValue is null
        && ProposalValidUntil is null
        && Notes is null;

    public static CommercialTerms Empty() => new();

    public static CommercialTerms Create(
        int? deliveryDays,
        string? paymentTerms,
        decimal? freightCost,
        decimal? minimumOrderValue,
        DateOnly? proposalValidUntil,
        string? notes)
    {
        if (deliveryDays is < CommercialTermsRules.MinimumDeliveryDays
            or > CommercialTermsRules.MaximumDeliveryDays)
        {
            throw new ArgumentOutOfRangeException(nameof(deliveryDays));
        }

        EnsureValidAmount(freightCost, nameof(freightCost));
        EnsureValidAmount(minimumOrderValue, nameof(minimumOrderValue));

        return new CommercialTerms
        {
            DeliveryDays = deliveryDays,
            PaymentTerms = DomainText.Optional(
                paymentTerms,
                CommercialTermsRules.PaymentTermsMaxLength,
                nameof(paymentTerms)),
            FreightCost = freightCost,
            MinimumOrderValue = minimumOrderValue,
            ProposalValidUntil = proposalValidUntil,
            Notes = DomainText.Optional(
                notes,
                CommercialTermsRules.NotesMaxLength,
                nameof(notes))
        };
    }

    private static void EnsureValidAmount(decimal? amount, string parameterName)
    {
        if (amount is null)
        {
            return;
        }

        if (amount < CommercialTermsRules.MinimumAmount
            || amount > CommercialTermsRules.MaximumAmount
            || decimal.Round(amount.Value, CommercialTermsRules.AmountScale) != amount.Value)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
