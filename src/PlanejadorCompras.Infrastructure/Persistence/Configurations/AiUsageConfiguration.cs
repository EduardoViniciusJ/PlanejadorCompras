using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Infrastructure.Persistence.Configurations;

public sealed class AiUsageConfiguration : IEntityTypeConfiguration<AiUsage>
{
    public void Configure(EntityTypeBuilder<AiUsage> builder)
    {
        builder.ToTable("AiUsages");

        builder.HasKey(usage => usage.Id);

        builder.Property(usage => usage.PeriodStart)
            .IsRequired();

        builder.Property(usage => usage.UsedCount)
            .IsRequired();

        builder.Property(usage => usage.UpdatedAt)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(usage => usage.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One counter per user and month; the atomic upsert in AiUsageStore relies on it.
        builder.HasIndex(usage => new { usage.UserId, usage.PeriodStart })
            .IsUnique();
    }
}
