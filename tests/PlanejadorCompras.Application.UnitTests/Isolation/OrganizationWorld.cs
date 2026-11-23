using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Infrastructure.Organizations;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Application.UnitTests.Isolation;

/// <summary>
/// Two organizations (A and B) sharing one database, each with one row of
/// every aggregate root and child. Tests open a context "as" one organization
/// and try to reach the other one's data through the real repositories,
/// queries, access services and use cases.
/// </summary>
internal sealed class OrganizationWorld
{
    private readonly DbContextOptions<PlanejadorComprasDbContext> _options;

    public OrganizationWorld()
    {
        _options = new DbContextOptionsBuilder<PlanejadorComprasDbContext>()
            .UseInMemoryDatabase($"isolation-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        A = Seed("A");
        B = Seed("B");
    }

    public OrganizationData A { get; }

    public OrganizationData B { get; }

    public PlanejadorComprasDbContext ContextFor(Guid organizationId)
    {
        var scope = new OrganizationContext();
        scope.Set(organizationId, OrganizationRole.Admin);
        return new PlanejadorComprasDbContext(_options, scope);
    }

    /// <summary>A context with no organization at all (anonymous request, background work).</summary>
    public PlanejadorComprasDbContext ContextWithoutOrganization() => new(_options);

    private OrganizationData Seed(string label)
    {
        var nowUtc = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var organization = Organization.Create($"Organização {label}", nowUtc);
        var userId = Guid.NewGuid();

        var list = ShoppingList.Create(userId, $"Lista {label}");
        var item = ShoppingItem.Create(list.Id, $"Item {label}", 2, "un");
        var supplier = Supplier.Create(userId, $"Fornecedor {label}");
        var link = ShoppingListSupplier.Create(list.Id, supplier.Id);
        var quote = ItemQuote.Create(item.Id, supplier.Id, 10m);
        var quotationRequest = QuotationRequest.Create(
            userId,
            list.Id,
            "SC-2026-00001",
            list.Name,
            null,
            $"Comprador {label}",
            $"comprador-{label.ToLowerInvariant()}@exemplo.com",
            null,
            null,
            null,
            [new QuotationRequestItemSnapshot(item.Id, item.Name, item.Quantity, item.Unit)],
            nowUtc);
        var equalization = SavedEqualization.Create(
            userId,
            Guid.NewGuid(),
            list.Id,
            "EQ-2026-00001",
            list.Name,
            $"Comprador {label}",
            $"comprador-{label.ToLowerInvariant()}@exemplo.com",
            20m,
            supplier.Name,
            20m,
            0m,
            [new SavedEqualizationItemSnapshot(
                item.Id,
                0,
                item.Name,
                item.Quantity,
                item.Unit,
                [new SavedEqualizationQuoteSnapshot(supplier.Id, supplier.Name, 10m, true)])],
            nowUtc);
        var purchaseOrder = PurchaseOrder.Create(
            userId,
            list.Id,
            supplier.Id,
            "PC-2026-00001",
            list.Name,
            supplier.Name,
            $"Comprador {label}",
            null,
            null,
            null,
            null,
            null,
            [new PurchaseOrderItemSnapshot(item.Id, item.Name, item.Quantity, item.Unit, 10m)],
            nowUtc);

        using var context = ContextFor(organization.Id);
        context.Organizations.Add(organization);
        context.AddRange(
            list,
            item,
            supplier,
            link,
            quote,
            quotationRequest,
            equalization,
            purchaseOrder);
        context.SaveChanges();

        return new OrganizationData(
            organization.Id,
            userId,
            list.Id,
            item.Id,
            supplier.Id,
            quote.Id,
            quotationRequest.Id,
            equalization.Id,
            purchaseOrder.Id);
    }
}

internal sealed record OrganizationData(
    Guid OrganizationId,
    Guid UserId,
    Guid ListId,
    Guid ItemId,
    Guid SupplierId,
    Guid QuoteId,
    Guid QuotationRequestId,
    Guid EqualizationId,
    Guid PurchaseOrderId);
