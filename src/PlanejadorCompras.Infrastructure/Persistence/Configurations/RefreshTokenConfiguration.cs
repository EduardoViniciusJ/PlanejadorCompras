using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Security;

namespace PlanejadorCompras.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(SecretToken.HashLength);

        builder.Property(token => token.DisplayName)
            .IsRequired()
            .HasMaxLength(RefreshToken.DisplayNameMaxLength);

        builder.Property(token => token.CreatedAt)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.Property(token => token.RevocationReason)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Ignore(token => token.IsRevoked);

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.HasIndex(token => token.SessionId);

        builder.HasIndex(token => token.UserId);
    }
}
