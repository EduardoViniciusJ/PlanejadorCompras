using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.Ai;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.ItemQuote;
using PlanejadorCompras.Domain.Repositories.ShoppingItem;
using PlanejadorCompras.Domain.Repositories.ShoppingListSupplier;
using ItemQuoteEntity = PlanejadorCompras.Domain.Entities.ItemQuote;
using ShoppingItemEntity = PlanejadorCompras.Domain.Entities.ShoppingItem;
using ShoppingListEntity = PlanejadorCompras.Domain.Entities.ShoppingList;
using SupplierEntity = PlanejadorCompras.Domain.Entities.Supplier;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Ai;

public sealed class ApplyQuoteBatchUseCaseTests
{
    private static readonly Guid ListId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly Mock<IShoppingListAccessService> _listAccess = new();
    private readonly Mock<ISupplierAccessService> _supplierAccess = new();
    private readonly Mock<IShoppingListSupplierRepository> _links = new();
    private readonly Mock<IShoppingItemRepository> _items = new();
    private readonly Mock<IItemQuoteRepository> _quotes = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SupplierEntity _supplier = SupplierEntity.Create(AiTestSetup.UserId, "Atacado Bom Preço");
    private readonly ShoppingItemEntity _rice = ShoppingItemEntity.Create(ListId, "Arroz", 20, "pct");
    private readonly ShoppingItemEntity _milk = ShoppingItemEntity.Create(ListId, "Leite", 120, "un");
    private readonly List<ItemQuoteEntity> _existingQuotes = [];
    private readonly List<ItemQuoteEntity> _added = [];

    public ApplyQuoteBatchUseCaseTests()
    {
        _listAccess
            .Setup(service => service.GetForCurrentUserAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ShoppingListEntity.Create(AiTestSetup.UserId, "Reposição"));
        _supplierAccess
            .Setup(service => service.GetForCurrentUserAsync(_supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_supplier);
        _links
            .Setup(repository => repository.ExistsAsync(ListId, _supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _items
            .Setup(repository => repository.GetByShoppingListIdAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([_rice, _milk]);
        _quotes
            .Setup(repository => repository.GetByShoppingListIdAsync(ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _existingQuotes);
        _quotes
            .Setup(repository => repository.AddAsync(It.IsAny<ItemQuoteEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ItemQuoteEntity, CancellationToken>((quote, _) => _added.Add(quote))
            .Returns(Task.CompletedTask);
    }

    private ApplyQuoteBatchUseCase Create() =>
        new(_listAccess.Object, _supplierAccess.Object, _links.Object, _items.Object, _quotes.Object, _unitOfWork.Object);

    private QuoteBatchRequestDto Request(params (Guid ItemId, decimal Price)[] lines) =>
        new(_supplier.Id, lines.Select(line => new QuoteBatchLineDto(line.ItemId, line.Price)).ToList());

    [Fact]
    public async Task ExecuteAsync_CreatesConfirmedPricesInOneCommit()
    {
        var response = await Create().ExecuteAsync(ListId, Request((_rice.Id, 24.90m), (_milk.Id, 4.99m)));

        Assert.Equal(new QuoteBatchResponseDto(2, 0), response);
        Assert.Equal(2, _added.Count);
        Assert.All(_added, quote => Assert.Equal(_supplier.Id, quote.SupplierId));
        Assert.Contains(_added, quote => quote.ShoppingItemId == _rice.Id && quote.UnitPrice == 24.90m);
        _unitOfWork.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesTheExistingPriceInsteadOfDuplicating()
    {
        var existing = ItemQuoteEntity.Create(_rice.Id, _supplier.Id, 30m);
        var fromOtherSupplier = ItemQuoteEntity.Create(_rice.Id, Guid.NewGuid(), 99m);
        _existingQuotes.AddRange([existing, fromOtherSupplier]);

        var response = await Create().ExecuteAsync(ListId, Request((_rice.Id, 24.90m), (_milk.Id, 4.99m)));

        Assert.Equal(new QuoteBatchResponseDto(1, 1), response);
        Assert.Equal(24.90m, existing.UnitPrice);
        Assert.Equal(99m, fromOtherSupplier.UnitPrice);
        Assert.Single(_added);
        _quotes.Verify(repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsTheSameItemTwice()
    {
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            Create().ExecuteAsync(ListId, Request((_rice.Id, 24.90m), (_rice.Id, 25m))));

        Assert.Equal("duplicate_quote_item", exception.ErrorCode);
        AssertNothingSaved();
    }

    [Fact]
    public async Task ExecuteAsync_RejectsItemsThatAreNotInTheList()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            Create().ExecuteAsync(ListId, Request((_rice.Id, 24.90m), (Guid.NewGuid(), 5m))));

        Assert.Equal("shopping_item_not_found", exception.ErrorCode);
        AssertNothingSaved();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteAsync_RejectsNonPositivePrices_AndSavesNothing(decimal price)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Create().ExecuteAsync(ListId, Request((_rice.Id, 24.90m), (_milk.Id, price))));

        AssertNothingSaved();
    }

    [Fact]
    public async Task ExecuteAsync_RejectsSupplierNotLinkedToTheList()
    {
        _links
            .Setup(repository => repository.ExistsAsync(ListId, _supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            Create().ExecuteAsync(ListId, Request((_rice.Id, 24.90m))));

        Assert.Equal("shopping_list_supplier_not_found", exception.ErrorCode);
        AssertNothingSaved();
    }

    [Fact]
    public async Task ExecuteAsync_RejectsListOfAnotherUser()
    {
        _listAccess
            .Setup(service => service.GetForCurrentUserAsync(ListId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Shopping list not found.", "shopping_list_not_found"));

        await Assert.ThrowsAsync<NotFoundException>(() => Create().ExecuteAsync(ListId, Request((_rice.Id, 24.90m))));

        AssertNothingSaved();
    }

    [Fact]
    public async Task ExecuteAsync_RejectsSupplierOfAnotherUser()
    {
        _supplierAccess
            .Setup(service => service.GetForCurrentUserAsync(_supplier.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Supplier not found.", "supplier_not_found"));

        await Assert.ThrowsAsync<NotFoundException>(() => Create().ExecuteAsync(ListId, Request((_rice.Id, 24.90m))));

        AssertNothingSaved();
    }

    [Fact]
    public async Task ExecuteAsync_RejectsEmptyBatch()
    {
        var exception = await Assert.ThrowsAsync<BadRequestException>(() => Create().ExecuteAsync(ListId, Request()));

        Assert.Equal("invalid_quote_batch", exception.ErrorCode);
        AssertNothingSaved();
    }

    private void AssertNothingSaved()
    {
        Assert.Empty(_added);
        _unitOfWork.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
