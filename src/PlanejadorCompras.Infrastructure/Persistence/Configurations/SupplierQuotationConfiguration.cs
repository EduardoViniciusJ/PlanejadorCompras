using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Infrastructure.Persistence.Configurations;

public sealed class SupplierQuotationConfiguration : IEntityTypeConfiguration<SupplierQuotation>
{
    public void Configure(EntityTypeBuilder<SupplierQuotation> builder)
    {
        builder.ToTable("SupplierQuotations");
        builder.HasKey(quotation => quotation.Id);
        // The id is created by the domain: an entity reached through a tracked request
        // must be inserted, not treated as an existing row.
        builder.Property(quotation => quotation.Id).ValueGeneratedNever();
        builder.Property(quotation => quotation.SupplierName)
            .IsRequired()
            .HasMaxLength(SupplierQuotationRules.SupplierNameMaxLength);
        builder.Property(quotation => quotation.SupplierEmail)
            .HasMaxLength(SupplierQuotationRules.SupplierEmailMaxLength);
        builder.Property(quotation => quotation.SupplierPhone)
            .HasMaxLength(SupplierQuotationRules.SupplierPhoneMaxLength);
        builder.Property(quotation => quotation.TokenHash)
            .IsRequired()
            .IsFixedLength()
            .HasMaxLength(SupplierQuotationRules.TokenHashLength);
        builder.Property(quotation => quotation.TokenExpiresAtUtc).IsRequired();
        builder.Property(quotation => quotation.CreatedAtUtc).IsRequired();
        builder.ComplexProperty(quotation => quotation.Terms, CommercialTermsConfiguration.Configure);
        builder.Ignore(quotation => quotation.Status);
        builder.Ignore(quotation => quotation.IsRevoked);
        builder.HasIndex(quotation => quotation.TokenHash).IsUnique();
        builder.HasIndex(quotation => new { quotation.QuotationRequestId, quotation.SupplierId })
            .IsUnique();
        builder.HasIndex(quotation => quotation.SupplierId);
        builder.HasMany(quotation => quotation.ResponseItems)
            .WithOne()
            .HasForeignKey(item => item.SupplierQuotationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(quotation => quotation.ResponseItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class SupplierQuotationResponseItemConfiguration
    : IEntityTypeConfiguration<SupplierQuotationResponseItem>
{
    public void Configure(EntityTypeBuilder<SupplierQuotationResponseItem> builder)
    {
        builder.ToTable("SupplierQuotationResponseItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.UnitPrice)
            .HasPrecision(ItemQuoteRules.UnitPricePrecision, ItemQuoteRules.UnitPriceScale)
            .IsRequired();
        builder.HasIndex(item => new { item.SupplierQuotationId, item.QuotationRequestItemId })
            .IsUnique();
        builder.HasOne<QuotationRequestItem>()
            .WithMany()
            .HasForeignKey(item => item.QuotationRequestItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
