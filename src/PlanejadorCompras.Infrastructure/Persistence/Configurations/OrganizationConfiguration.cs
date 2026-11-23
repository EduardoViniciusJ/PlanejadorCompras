using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Name)
            .IsRequired()
            .HasMaxLength(OrganizationRules.NameMaxLength);

        builder.Property(organization => organization.CreatedAtUtc)
            .IsRequired();
    }
}

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");

        builder.HasKey(membership => new { membership.OrganizationId, membership.UserId });

        builder.Property(membership => membership.Role)
            .IsRequired();

        builder.Property(membership => membership.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(membership => membership.UserId);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(membership => membership.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrganizationInvitationConfiguration
    : IEntityTypeConfiguration<OrganizationInvitation>
{
    public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
    {
        builder.ToTable("OrganizationInvitations");

        builder.HasKey(invitation => invitation.Id);

        builder.Property(invitation => invitation.Email)
            .IsRequired()
            .HasColumnType("citext")
            .HasMaxLength(OrganizationRules.EmailMaxLength);

        builder.Property(invitation => invitation.Role)
            .IsRequired();

        builder.Property(invitation => invitation.TokenHash)
            .IsRequired()
            .HasMaxLength(OrganizationRules.InvitationTokenHashLength);

        builder.Property(invitation => invitation.CreatedAtUtc)
            .IsRequired();

        builder.Property(invitation => invitation.ExpiresAtUtc)
            .IsRequired();

        builder.HasIndex(invitation => invitation.TokenHash)
            .IsUnique();

        builder.HasIndex(invitation => new { invitation.OrganizationId, invitation.Email })
            .IsUnique();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(invitation => invitation.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invitation => invitation.InvitedByUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OrganizationDocumentCounterConfiguration
    : IEntityTypeConfiguration<OrganizationDocumentCounter>
{
    public void Configure(EntityTypeBuilder<OrganizationDocumentCounter> builder)
    {
        builder.ToTable("OrganizationDocumentCounters");

        builder.HasKey(counter => new { counter.OrganizationId, counter.Kind, counter.Year });

        builder.Property(counter => counter.Kind)
            .HasMaxLength(4);

        builder.Property(counter => counter.LastNumber)
            .IsRequired();

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(counter => counter.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
