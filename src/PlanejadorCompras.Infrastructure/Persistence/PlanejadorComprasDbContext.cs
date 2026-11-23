using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Infrastructure.Organizations;

namespace PlanejadorCompras.Infrastructure.Persistence;

public sealed class PlanejadorComprasDbContext : DbContext
{
    private readonly IOrganizationScope? _organizationScope;

    public PlanejadorComprasDbContext(
        DbContextOptions<PlanejadorComprasDbContext> options,
        IOrganizationScope? organizationScope = null)
        : base(options)
    {
        _organizationScope = organizationScope;
    }

    /// <summary>
    /// Organization every query and write of this context is confined to.
    /// Null (anonymous request, migrations, background work) matches no
    /// organization-owned row at all.
    /// </summary>
    public Guid? CurrentOrganizationId => _organizationScope?.OrganizationId;

    public DbSet<User> Users => Set<User>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<OrganizationInvitation> OrganizationInvitations => Set<OrganizationInvitation>();

    public DbSet<OrganizationDocumentCounter> OrganizationDocumentCounters =>
        Set<OrganizationDocumentCounter>();

    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();

    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();

    public DbSet<ItemQuote> ItemQuotes => Set<ItemQuote>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<ShoppingListSupplier> ShoppingListSuppliers => Set<ShoppingListSupplier>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    public DbSet<QuotationRequest> QuotationRequests => Set<QuotationRequest>();

    public DbSet<QuotationRequestItem> QuotationRequestItems => Set<QuotationRequestItem>();

    public DbSet<SavedEqualization> SavedEqualizations => Set<SavedEqualization>();

    public DbSet<SavedEqualizationItem> SavedEqualizationItems => Set<SavedEqualizationItem>();

    public DbSet<SavedEqualizationQuote> SavedEqualizationQuotes => Set<SavedEqualizationQuote>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceOrganizationOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceOrganizationOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanejadorComprasDbContext).Assembly);
        ApplyOrganizationOwnership<ShoppingList>(modelBuilder);
        ApplyOrganizationOwnership<ShoppingItem>(modelBuilder);
        ApplyOrganizationOwnership<ItemQuote>(modelBuilder);
        ApplyOrganizationOwnership<Supplier>(modelBuilder);
        ApplyOrganizationOwnership<ShoppingListSupplier>(modelBuilder);
        ApplyOrganizationOwnership<PurchaseOrder>(modelBuilder);
        ApplyOrganizationOwnership<QuotationRequest>(modelBuilder);
        ApplyOrganizationOwnership<SavedEqualization>(modelBuilder);
        EnsureEveryOwnedEntityIsFiltered(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Tenant filter plus the organization foreign key. The filter reads
    /// <see cref="CurrentOrganizationId"/> from the context instance running the
    /// query, so the cached model is safe to share between requests.
    /// </summary>
    private void ApplyOrganizationOwnership<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IOrganizationOwned
    {
        var entity = modelBuilder.Entity<TEntity>();

        entity.HasQueryFilter(owned => (Guid?)owned.OrganizationId == CurrentOrganizationId);
        entity.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(owned => owned.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Fails at start-up if someone maps a new organization-owned entity and
    /// forgets to register its filter above.
    /// </summary>
    private static void EnsureEveryOwnedEntityIsFiltered(ModelBuilder modelBuilder)
    {
        var unfiltered = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => typeof(IOrganizationOwned).IsAssignableFrom(entityType.ClrType))
            .Where(entityType => entityType.GetQueryFilter() is null)
            .Select(entityType => entityType.ClrType.Name)
            .ToList();

        if (unfiltered.Count > 0)
        {
            throw new InvalidOperationException(
                "Organization-owned entities without a query filter: "
                + string.Join(", ", unfiltered));
        }
    }

    /// <summary>
    /// Last line of defence for writes: new rows are stamped with the current
    /// organization and any attempt to insert, change or delete a row of
    /// another organization aborts the whole save.
    /// </summary>
    private void EnforceOrganizationOwnership()
    {
        var entries = ChangeTracker.Entries<IOrganizationOwned>()
            .Where(entry => entry.State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var currentOrganizationId = CurrentOrganizationId
            ?? throw new InvalidOperationException(
                "Organization-owned data cannot be written without a current organization.");

        foreach (var entry in entries)
        {
            var property = entry.Property(nameof(IOrganizationOwned.OrganizationId));

            if (entry.State == EntityState.Added)
            {
                StampOrVerifyNewRow(entry, property, currentOrganizationId);
                continue;
            }

            var originalOrganizationId = (Guid)property.OriginalValue!;
            var newOrganizationId = (Guid)property.CurrentValue!;

            if (originalOrganizationId != currentOrganizationId
                || newOrganizationId != currentOrganizationId)
            {
                throw CrossOrganizationWrite(entry);
            }
        }
    }

    private static void StampOrVerifyNewRow(
        EntityEntry entry,
        PropertyEntry property,
        Guid currentOrganizationId)
    {
        var organizationId = (Guid)property.CurrentValue!;

        if (organizationId == Guid.Empty)
        {
            property.CurrentValue = currentOrganizationId;
            return;
        }

        if (organizationId != currentOrganizationId)
        {
            throw CrossOrganizationWrite(entry);
        }
    }

    private static InvalidOperationException CrossOrganizationWrite(EntityEntry entry) =>
        new($"Blocked a write to {entry.Metadata.ClrType.Name} that belongs to another organization.");
}
