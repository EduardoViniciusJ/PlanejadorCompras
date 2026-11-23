using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Infrastructure.Persistence.Configurations;

internal static class CommercialTermsConfiguration
{
    internal static void Configure(ComplexPropertyBuilder<CommercialTerms> builder)
    {
        builder.Property(terms => terms.DeliveryDays).HasColumnName("TermsDeliveryDays");
        builder.Property(terms => terms.PaymentTerms)
            .HasColumnName("TermsPaymentTerms")
            .HasMaxLength(CommercialTermsRules.PaymentTermsMaxLength);
        builder.Property(terms => terms.FreightCost)
            .HasColumnName("TermsFreightCost")
            .HasPrecision(CommercialTermsRules.AmountPrecision, CommercialTermsRules.AmountScale);
        builder.Property(terms => terms.MinimumOrderValue)
            .HasColumnName("TermsMinimumOrderValue")
            .HasPrecision(CommercialTermsRules.AmountPrecision, CommercialTermsRules.AmountScale);
        builder.Property(terms => terms.ProposalValidUntil)
            .HasColumnName("TermsProposalValidUntil")
            .HasColumnType("date");
        builder.Property(terms => terms.Notes)
            .HasColumnName("TermsNotes")
            .HasMaxLength(CommercialTermsRules.NotesMaxLength);
        builder.Ignore(terms => terms.IsEmpty);
    }
}
