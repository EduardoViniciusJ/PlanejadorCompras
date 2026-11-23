using PlanejadorCompras.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanejadorCompras.Domain.Entities;

namespace PlanejadorCompras.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.GoogleId)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(user => user.PasswordHash)
            .IsRequired(false)
            .HasMaxLength(255);

        builder.Property(user => user.IsEmailConfirmed)
            .IsRequired();

        builder.Property(user => user.Email)
            .IsRequired()
            .HasColumnType("citext")
            .HasMaxLength(320);

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.Property(user => user.Name)
            .HasMaxLength(OrganizationRules.UserNameMaxLength);

        builder.Property(user => user.TermsVersion)
            .HasMaxLength(OrganizationRules.TermsVersionMaxLength);

        builder.Ignore(user => user.IsDeleted);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(user => user.ActiveOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(user => user.GoogleId)
            .IsUnique();

        builder.HasIndex(user => user.Email)
            .IsUnique();
    }
}
