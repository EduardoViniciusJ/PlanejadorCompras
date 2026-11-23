using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.ItemQuotes.Contracts;
using PlanejadorCompras.Application.Features.PurchaseOrders.Contracts;
using PlanejadorCompras.Application.Features.ShoppingItems.Contracts;
using PlanejadorCompras.Application.Features.ShoppingLists.Contracts;
using PlanejadorCompras.Application.Features.Suppliers.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UnitTests.Support;
using PlanejadorCompras.Application.UseCases.Equalization;
using PlanejadorCompras.Application.UseCases.ItemQuote;
using PlanejadorCompras.Application.UseCases.ItemQuote.Create;
using PlanejadorCompras.Application.UseCases.PurchaseOrder;
using PlanejadorCompras.Application.UseCases.QuotationRequest;
using PlanejadorCompras.Application.UseCases.ShoppingItem;
using PlanejadorCompras.Application.UseCases.ShoppingList;
using PlanejadorCompras.Application.UseCases.Supplier;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Infrastructure.Persistence;
using PlanejadorCompras.Infrastructure.Queries;
using PlanejadorCompras.Infrastructure.Repositories;
using PlanejadorCompras.Infrastructure.Services;

namespace PlanejadorCompras.Application.UnitTests.Isolation;

/// <summary>
/// Proves that a user working in organization A cannot read or change
/// anything of organization B, on every aggregate root, going through the
/// same repositories, queries, access services and use cases the API uses.
/// </summary>
public sealed class OrganizationIsolationTests
{
    private readonly OrganizationWorld _world = new();

    // ------------------------------------------------------------------ reads

    [Fact]
    public async Task Repositories_ShouldNotFindRowsOfAnotherOrganizationById()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var other = _world.B;

        Assert.Null(await new ShoppingListRepository(context).GetByIdAsync(other.ListId));
        Assert.Null(await new ShoppingItemRepository(context).GetByIdAsync(other.ItemId));
        Assert.Null(await new ItemQuoteRepository(context).GetByIdAsync(other.QuoteId));
        Assert.Null(await new SupplierRepository(context).GetByIdAsync(other.SupplierId));
        Assert.Null(await new PurchaseOrderRepository(context).GetByIdAsync(other.PurchaseOrderId));
        Assert.Null(await new SavedEqualizationRepository(context).GetByIdAsync(other.EqualizationId));
        Assert.Null(await new QuotationRequestRepository(context)
            .GetByIdForOrganizationAsync(other.QuotationRequestId, _world.A.OrganizationId));
    }

    [Fact]
    public async Task Repositories_ShouldFindTheirOwnRows()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var own = _world.A;

        Assert.NotNull(await new ShoppingListRepository(context).GetByIdAsync(own.ListId));
        Assert.NotNull(await new ShoppingItemRepository(context).GetByIdAsync(own.ItemId));
        Assert.NotNull(await new ItemQuoteRepository(context).GetByIdAsync(own.QuoteId));
        Assert.NotNull(await new SupplierRepository(context).GetByIdAsync(own.SupplierId));
        Assert.NotNull(await new PurchaseOrderRepository(context).GetByIdAsync(own.PurchaseOrderId));
        Assert.NotNull(await new SavedEqualizationRepository(context).GetByIdAsync(own.EqualizationId));
        Assert.NotNull(await new QuotationRequestRepository(context)
            .GetByIdForOrganizationAsync(own.QuotationRequestId, own.OrganizationId));
    }

    [Fact]
    public async Task Repositories_ShouldReturnNothing_EvenWhenAskedExplicitlyForAnotherOrganization()
    {
        // A caller bug (or a forged id) passing organization B to a context that
        // belongs to organization A must still come back empty.
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var other = _world.B;

        Assert.Empty(await new ShoppingListRepository(context).GetByOrganizationIdAsync(other.OrganizationId));
        Assert.Empty(await new SupplierRepository(context).GetByOrganizationIdAsync(other.OrganizationId));
        Assert.Empty(await new PurchaseOrderRepository(context).GetByOrganizationIdAsync(other.OrganizationId));
        Assert.Empty(await new QuotationRequestRepository(context).GetByOrganizationIdAsync(other.OrganizationId));
        Assert.Null(await new QuotationRequestRepository(context)
            .GetByIdForOrganizationAsync(other.QuotationRequestId, other.OrganizationId));
        Assert.False(await new QuotationRequestRepository(context)
            .DeleteForOrganizationAsync(other.QuotationRequestId, other.OrganizationId));

        var (equalizations, total) = await new SavedEqualizationRepository(context)
            .SearchByOrganizationIdAsync(other.OrganizationId, null, 1, 10);
        Assert.Empty(equalizations);
        Assert.Equal(0, total);

        Assert.Empty(await new ShoppingListOverviewQuery(context).GetByOrganizationIdAsync(other.OrganizationId));
        Assert.Empty(await new UserItemQuotesQuery(context).GetByOrganizationIdAsync(other.OrganizationId));
        Assert.Null(await new ShoppingListDetailQuery(context).GetByIdAsync(other.OrganizationId, other.ListId));
    }

    [Fact]
    public async Task Listings_ShouldContainOnlyRowsOfTheCurrentOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var own = _world.A;

        Assert.Equal([own.ListId], (await new ShoppingListRepository(context)
            .GetByOrganizationIdAsync(own.OrganizationId)).Select(list => list.Id));
        Assert.Equal([own.SupplierId], (await new SupplierRepository(context)
            .GetByOrganizationIdAsync(own.OrganizationId)).Select(supplier => supplier.Id));
        Assert.Equal([own.PurchaseOrderId], (await new PurchaseOrderRepository(context)
            .GetByOrganizationIdAsync(own.OrganizationId)).Select(order => order.Id));
        Assert.Equal([own.QuotationRequestId], (await new QuotationRequestRepository(context)
            .GetByOrganizationIdAsync(own.OrganizationId)).Select(request => request.Id));
        Assert.Equal([own.EqualizationId], (await new SavedEqualizationRepository(context)
            .SearchByOrganizationIdAsync(own.OrganizationId, null, 1, 10)).Items.Select(row => row.Id));
        Assert.Equal([own.ListId], (await new ShoppingListOverviewQuery(context)
            .GetByOrganizationIdAsync(own.OrganizationId)).Select(list => list.Id));
        Assert.Equal([own.QuoteId], (await new UserItemQuotesQuery(context)
            .GetByOrganizationIdAsync(own.OrganizationId)).Select(quote => quote.Id));
    }

    [Fact]
    public async Task ChildRows_ShouldNotBeReachableThroughTheParentIdOfAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var other = _world.B;

        Assert.Empty(await new ShoppingItemRepository(context).GetByShoppingListIdAsync(other.ListId));
        Assert.Empty(await new ItemQuoteRepository(context).GetByShoppingListIdAsync(other.ListId));
        Assert.Empty(await new ItemQuoteRepository(context).GetByShoppingItemIdAsync(other.ItemId));
        Assert.Empty(await new ShoppingListSupplierRepository(context).GetSuppliersAsync(other.ListId));
        Assert.False(await new ShoppingListSupplierRepository(context)
            .ExistsAsync(other.ListId, other.SupplierId));
        Assert.Empty(await new SupplierRepository(context).GetByIdsAsync([other.SupplierId]));
        Assert.False(await new SupplierRepository(context).HasQuotesAsync(other.SupplierId));
    }

    [Fact]
    public async Task UniquenessChecks_ShouldIgnoreNamesUsedByAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var repository = new SupplierRepository(context);

        Assert.False(await repository.ExistsByNameAsync(_world.A.OrganizationId, "Fornecedor B"));
        Assert.True(await repository.ExistsByNameAsync(_world.A.OrganizationId, "Fornecedor A"));
        Assert.False(await new PurchaseOrderRepository(context).ExistsForSourceAsync(
            _world.A.OrganizationId,
            _world.B.ListId,
            _world.B.SupplierId));
    }

    [Fact]
    public async Task AccessServices_ShouldAnswerNotFoundForAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var organization = TestOrganization.For(_world.A.OrganizationId);
        var other = _world.B;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ShoppingListAccessService(new ShoppingListRepository(context), organization)
                .GetForCurrentUserAsync(other.ListId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new SupplierAccessService(new SupplierRepository(context), organization)
                .GetForCurrentUserAsync(other.SupplierId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new PurchaseOrderAccessService(new PurchaseOrderRepository(context), organization)
                .GetForCurrentUserAsync(other.PurchaseOrderId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new SavedEqualizationAccessService(new SavedEqualizationRepository(context), organization)
                .GetForCurrentUserAsync(other.EqualizationId));
    }

    [Fact]
    public async Task AccessServices_ShouldRejectARowOfAnotherOrganization_EvenIfTheRepositoryReturnedIt()
    {
        // Second, independent check: the row is loaded by a context of B and
        // handed to an access service that works for A.
        await using var contextOfB = _world.ContextFor(_world.B.OrganizationId);
        var organizationA = TestOrganization.For(_world.A.OrganizationId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new ShoppingListAccessService(new ShoppingListRepository(contextOfB), organizationA)
                .GetForCurrentUserAsync(_world.B.ListId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new SupplierAccessService(new SupplierRepository(contextOfB), organizationA)
                .GetForCurrentUserAsync(_world.B.SupplierId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new PurchaseOrderAccessService(new PurchaseOrderRepository(contextOfB), organizationA)
                .GetForCurrentUserAsync(_world.B.PurchaseOrderId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new SavedEqualizationAccessService(new SavedEqualizationRepository(contextOfB), organizationA)
                .GetForCurrentUserAsync(_world.B.EqualizationId));
    }

    [Fact]
    public async Task ContextWithoutOrganization_ShouldSeeNoOrganizationData()
    {
        await using var context = _world.ContextWithoutOrganization();

        Assert.Empty(await context.ShoppingLists.ToListAsync());
        Assert.Empty(await context.ShoppingItems.ToListAsync());
        Assert.Empty(await context.ItemQuotes.ToListAsync());
        Assert.Empty(await context.Suppliers.ToListAsync());
        Assert.Empty(await context.ShoppingListSuppliers.ToListAsync());
        Assert.Empty(await context.PurchaseOrders.ToListAsync());
        Assert.Empty(await context.QuotationRequests.ToListAsync());
        Assert.Empty(await context.SavedEqualizations.ToListAsync());
    }

    [Fact]
    public async Task TheSameCachedModel_ShouldFilterByTheOrganizationOfEachContextInstance()
    {
        // The EF model is built once and shared; the filter must read the
        // organization from whichever context instance runs the query.
        await using var contextOfA = _world.ContextFor(_world.A.OrganizationId);
        await using var contextOfB = _world.ContextFor(_world.B.OrganizationId);

        Assert.Equal(_world.A.ListId, (await contextOfA.ShoppingLists.SingleAsync()).Id);
        Assert.Equal(_world.B.ListId, (await contextOfB.ShoppingLists.SingleAsync()).Id);
        Assert.Equal(_world.A.ListId, (await contextOfA.ShoppingLists.SingleAsync()).Id);
    }

    // ----------------------------------------------------------------- writes

    [Fact]
    public async Task NewRows_ShouldBeStampedWithTheCurrentOrganization()
    {
        var list = ShoppingList.Create(_world.A.UserId, "Nova lista");

        await using (var context = _world.ContextFor(_world.A.OrganizationId))
        {
            context.ShoppingLists.Add(list);
            await context.SaveChangesAsync();
        }

        Assert.Equal(_world.A.OrganizationId, list.OrganizationId);

        await using var contextOfB = _world.ContextFor(_world.B.OrganizationId);
        Assert.Null(await new ShoppingListRepository(contextOfB).GetByIdAsync(list.Id));
    }

    [Fact]
    public async Task Save_ShouldRejectANewRowAlreadyAssignedToAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        context.ShoppingLists.Add(
            ShoppingList.Create(_world.A.UserId, "Lista infiltrada")
                .InOrganization(_world.B.OrganizationId));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Save_ShouldRejectWritesWithoutACurrentOrganization()
    {
        await using var context = _world.ContextWithoutOrganization();
        context.Suppliers.Add(Supplier.Create(_world.A.UserId, "Sem organização"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Save_ShouldRejectChangingOrDeletingARowOfAnotherOrganization()
    {
        // Even if a row of B somehow reaches a context of A (attached by hand,
        // bypassing every repository), the save itself refuses it.
        ShoppingList listOfB;
        Supplier supplierOfB;
        await using (var contextOfB = _world.ContextFor(_world.B.OrganizationId))
        {
            listOfB = await contextOfB.ShoppingLists.AsNoTracking().SingleAsync();
            supplierOfB = await contextOfB.Suppliers.AsNoTracking().SingleAsync();
        }

        await using (var context = _world.ContextFor(_world.A.OrganizationId))
        {
            listOfB.Update("Nome trocado");
            context.ShoppingLists.Update(listOfB);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }

        await using (var context = _world.ContextFor(_world.A.OrganizationId))
        {
            context.Suppliers.Remove(supplierOfB);
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }

        await AssertOrganizationBIsIntactAsync();
    }

    [Fact]
    public async Task Save_ShouldRejectMovingARowToAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var list = await context.ShoppingLists.SingleAsync();
        list.InOrganization(_world.B.OrganizationId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    // -------------------------------------------------------------- use cases

    [Fact]
    public async Task ShoppingListUseCases_ShouldNotTouchAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var services = new Services(context, _world.A);
        var other = _world.B;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetShoppingListDetailUseCase(services.Organization, new ShoppingListDetailQuery(context))
                .ExecuteAsync(other.ListId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateShoppingListUseCase(services.Lists, services.UnitOfWork, services.ListAccess)
                .ExecuteAsync(other.ListId, new ShoppingListRequestDto("Invadida", null)));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteShoppingListUseCase(services.Lists, services.UnitOfWork, services.ListAccess)
                .ExecuteAsync(other.ListId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new AddSupplierToShoppingListUseCase(
                    services.Links,
                    services.ListAccess,
                    services.SupplierAccess,
                    services.UnitOfWork)
                .ExecuteAsync(_world.A.ListId, other.SupplierId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new AddSupplierToShoppingListUseCase(
                    services.Links,
                    services.ListAccess,
                    services.SupplierAccess,
                    services.UnitOfWork)
                .ExecuteAsync(other.ListId, _world.A.SupplierId));

        await AssertOrganizationBIsIntactAsync();
    }

    [Fact]
    public async Task ShoppingItemUseCases_ShouldNotTouchAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var services = new Services(context, _world.A);
        var other = _world.B;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetShoppingItemsByShoppingListIdUseCase(services.Items, services.ListAccess)
                .ExecuteAsync(other.ListId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateShoppingItemUseCase(services.Items, services.UnitOfWork, services.ListAccess)
                .ExecuteAsync(other.ItemId, new ShoppingItemRequestDto(other.ListId, "Trocado", 9, "kg")));
        // Stealing an item of B into a list of A.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateShoppingItemUseCase(services.Items, services.UnitOfWork, services.ListAccess)
                .ExecuteAsync(other.ItemId, new ShoppingItemRequestDto(_world.A.ListId, "Roubado", 9, "kg")));
        // Pushing an item of A into a list of B.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateShoppingItemUseCase(services.Items, services.UnitOfWork, services.ListAccess)
                .ExecuteAsync(_world.A.ItemId, new ShoppingItemRequestDto(other.ListId, "Empurrado", 9, "kg")));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteShoppingItemUseCase(services.Items, services.UnitOfWork, services.ListAccess)
                .ExecuteAsync(other.ItemId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteShoppingItemsUseCase(services.Items, services.UnitOfWork, services.ListAccess)
                .ExecuteAsync(_world.A.ListId, [other.ItemId]));

        await AssertOrganizationBIsIntactAsync();
    }

    [Fact]
    public async Task ItemQuoteUseCases_ShouldNotTouchAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var services = new Services(context, _world.A);
        var other = _world.B;

        // Price on an item of B.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            services.CreateItemQuote().ExecuteAsync(
                new ItemQuoteRequestDto(other.ItemId, other.SupplierId, 1m)));
        // Price on an own item using a supplier of B.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            services.CreateItemQuote().ExecuteAsync(
                new ItemQuoteRequestDto(_world.A.ItemId, other.SupplierId, 1m)));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteItemQuoteUseCase(
                    services.Quotes,
                    services.Items,
                    services.UnitOfWork,
                    services.ListAccess)
                .ExecuteAsync(other.QuoteId));

        await AssertOrganizationBIsIntactAsync();
    }

    [Fact]
    public async Task SupplierUseCases_ShouldNotTouchAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var services = new Services(context, _world.A);
        var other = _world.B;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetSupplierByIdUseCase(services.SupplierAccess).ExecuteAsync(other.SupplierId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateSupplierUseCase(services.Suppliers, services.UnitOfWork, services.SupplierAccess)
                .ExecuteAsync(other.SupplierId, new SupplierRequestDto("Invadido")));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteSupplierUseCase(services.Suppliers, services.UnitOfWork, services.SupplierAccess)
                .ExecuteAsync(other.SupplierId));

        var listed = await new GetSuppliersUseCase(services.Suppliers, services.Organization).ExecuteAsync();
        Assert.Equal(_world.A.SupplierId, Assert.Single(listed).Id);

        await AssertOrganizationBIsIntactAsync();
    }

    [Fact]
    public async Task SupplierNames_ShouldBeUniquePerOrganizationOnly()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var services = new Services(context, _world.A);

        // "Fornecedor B" exists in organization B; A may use the same name.
        var created = await new CreateSupplierUseCase(
                services.Suppliers,
                services.UnitOfWork,
                services.User,
                services.Organization)
            .ExecuteAsync(new SupplierRequestDto("Fornecedor B"));

        Assert.NotEqual(_world.B.SupplierId, created.Id);

        await Assert.ThrowsAsync<ConflictException>(() =>
            new CreateSupplierUseCase(
                    services.Suppliers,
                    services.UnitOfWork,
                    services.User,
                    services.Organization)
                .ExecuteAsync(new SupplierRequestDto("Fornecedor A")));
    }

    [Fact]
    public async Task QuotationRequestUseCases_ShouldNotTouchAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var services = new Services(context, _world.A);
        var other = _world.B;
        var repository = new QuotationRequestRepository(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetQuotationRequestByIdUseCase(repository, services.Organization)
                .ExecuteAsync(other.QuotationRequestId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteQuotationRequestUseCase(repository, services.Organization, services.UnitOfWork)
                .ExecuteAsync(other.QuotationRequestId));

        var listed = await new GetQuotationRequestsUseCase(repository, services.Organization).ExecuteAsync();
        Assert.Equal(_world.A.QuotationRequestId, Assert.Single(listed).Id);

        await AssertOrganizationBIsIntactAsync();
    }

    [Fact]
    public async Task EqualizationUseCases_ShouldNotTouchAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var services = new Services(context, _world.A);
        var other = _world.B;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetSavedEqualizationByIdUseCase(services.EqualizationAccess)
                .ExecuteAsync(other.EqualizationId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteSavedEqualizationUseCase(
                    services.Equalizations,
                    services.EqualizationAccess,
                    services.UnitOfWork)
                .ExecuteAsync(other.EqualizationId));

        var page = await new GetSavedEqualizationsUseCase(services.Equalizations, services.Organization)
            .ExecuteAsync(null, 1, 10);
        Assert.Equal(_world.A.EqualizationId, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalCount);

        await AssertOrganizationBIsIntactAsync();
    }

    [Fact]
    public async Task PurchaseOrderUseCases_ShouldNotTouchAnotherOrganization()
    {
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var services = new Services(context, _world.A);
        var other = _world.B;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetPurchaseOrderByIdUseCase(services.OrderAccess).ExecuteAsync(other.PurchaseOrderId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdatePurchaseOrderStatusUseCase(services.OrderAccess, services.UnitOfWork, TimeProvider.System)
                .ExecuteAsync(other.PurchaseOrderId, new UpdatePurchaseOrderStatusRequestDto("cancelled")));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeletePurchaseOrderUseCase(services.Orders, services.OrderAccess, services.UnitOfWork)
                .ExecuteAsync(other.PurchaseOrderId));

        var draftService = new PurchaseOrderDraftService(
            services.ListAccess,
            services.SupplierAccess,
            services.Links,
            services.Items,
            services.Quotes,
            services.EqualizationAccess);

        // Draft from a list, a supplier or a saved equalization of B.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            draftService.BuildAsync(other.ListId, other.SupplierId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            draftService.BuildAsync(_world.A.ListId, other.SupplierId));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            draftService.BuildAsync(_world.A.ListId, _world.A.SupplierId, other.EqualizationId));

        var listed = await new GetPurchaseOrdersUseCase(services.Orders, services.Organization).ExecuteAsync();
        Assert.Equal(_world.A.PurchaseOrderId, Assert.Single(listed).Id);

        await AssertOrganizationBIsIntactAsync();
    }

    [Fact]
    public async Task MembersOfTheSameOrganization_ShouldShareItsData()
    {
        // The counterpart of isolation: another member of A (different user id)
        // sees and edits what a colleague created.
        await using var context = _world.ContextFor(_world.A.OrganizationId);
        var colleague = _world.A with { UserId = Guid.NewGuid() };
        var services = new Services(context, colleague);

        var updated = await new UpdateShoppingListUseCase(
                services.Lists,
                services.UnitOfWork,
                services.ListAccess)
            .ExecuteAsync(_world.A.ListId, new ShoppingListRequestDto("Editada pelo colega", null));

        Assert.Equal("Editada pelo colega", updated.Name);
        Assert.Single(await new GetSuppliersUseCase(services.Suppliers, services.Organization).ExecuteAsync());
    }

    private async Task AssertOrganizationBIsIntactAsync()
    {
        await using var context = _world.ContextFor(_world.B.OrganizationId);
        var other = _world.B;

        Assert.Equal("Lista B", (await context.ShoppingLists.SingleAsync()).Name);
        Assert.Equal(other.ListId, (await context.ShoppingItems.SingleAsync()).ShoppingListId);
        Assert.Equal("Item B", (await context.ShoppingItems.SingleAsync()).Name);
        Assert.Equal("Fornecedor B", (await context.Suppliers.SingleAsync()).Name);
        Assert.Equal(10m, (await context.ItemQuotes.SingleAsync()).UnitPrice);
        Assert.Equal(other.SupplierId, (await context.ShoppingListSuppliers.SingleAsync()).SupplierId);
        Assert.Equal(other.QuotationRequestId, (await context.QuotationRequests.SingleAsync()).Id);
        Assert.Equal(other.EqualizationId, (await context.SavedEqualizations.SingleAsync()).Id);
        Assert.Equal(PurchaseOrderStatus.Issued, (await context.PurchaseOrders.SingleAsync()).Status);
    }

    /// <summary>The real infrastructure services, wired the way the API wires them.</summary>
    private sealed class Services
    {
        private readonly PlanejadorComprasDbContext _context;

        public Services(PlanejadorComprasDbContext context, OrganizationData actor)
        {
            _context = context;
            Organization = TestOrganization.For(actor.OrganizationId);

            var user = new Moq.Mock<ICurrentUser>();
            user.SetupGet(current => current.UserId).Returns(actor.UserId);
            user.SetupGet(current => current.Email).Returns("ator@exemplo.com");
            user.SetupGet(current => current.Name).Returns("Ator");
            User = user.Object;

            Lists = new ShoppingListRepository(context);
            Items = new ShoppingItemRepository(context);
            Quotes = new ItemQuoteRepository(context);
            Suppliers = new SupplierRepository(context);
            Links = new ShoppingListSupplierRepository(context);
            Orders = new PurchaseOrderRepository(context);
            Equalizations = new SavedEqualizationRepository(context);
            UnitOfWork = new UnitOfWork(context);
            ListAccess = new ShoppingListAccessService(Lists, Organization);
            SupplierAccess = new SupplierAccessService(Suppliers, Organization);
            OrderAccess = new PurchaseOrderAccessService(Orders, Organization);
            EqualizationAccess = new SavedEqualizationAccessService(Equalizations, Organization);
        }

        public ICurrentOrganization Organization { get; }
        public ICurrentUser User { get; }
        public ShoppingListRepository Lists { get; }
        public ShoppingItemRepository Items { get; }
        public ItemQuoteRepository Quotes { get; }
        public SupplierRepository Suppliers { get; }
        public ShoppingListSupplierRepository Links { get; }
        public PurchaseOrderRepository Orders { get; }
        public SavedEqualizationRepository Equalizations { get; }
        public UnitOfWork UnitOfWork { get; }
        public ShoppingListAccessService ListAccess { get; }
        public SupplierAccessService SupplierAccess { get; }
        public PurchaseOrderAccessService OrderAccess { get; }
        public SavedEqualizationAccessService EqualizationAccess { get; }

        public CreateItemQuoteUseCase CreateItemQuote() =>
            new(Quotes, Items, UnitOfWork, ListAccess, SupplierAccess, Links);
    }
}
