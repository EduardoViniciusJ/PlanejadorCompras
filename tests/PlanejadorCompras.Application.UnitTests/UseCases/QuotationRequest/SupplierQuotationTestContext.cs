using Moq;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.QuotationRequest;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.ItemQuote;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;
using PlanejadorCompras.Domain.Repositories.ShoppingItem;
using PlanejadorCompras.Domain.Repositories.ShoppingList;
using PlanejadorCompras.Domain.Repositories.ShoppingListSupplier;
using PlanejadorCompras.Domain.Repositories.Supplier;
using ItemQuoteEntity = PlanejadorCompras.Domain.Entities.ItemQuote;
using QuotationRequestEntity = PlanejadorCompras.Domain.Entities.QuotationRequest;
using ShoppingItemEntity = PlanejadorCompras.Domain.Entities.ShoppingItem;
using ShoppingListEntity = PlanejadorCompras.Domain.Entities.ShoppingList;
using ShoppingListSupplierEntity = PlanejadorCompras.Domain.Entities.ShoppingListSupplier;
using SupplierEntity = PlanejadorCompras.Domain.Entities.Supplier;

namespace PlanejadorCompras.Application.UnitTests.UseCases.QuotationRequest;

/// <summary>
/// In-memory world for the supplier quotation flow: a buyer with one list (two items),
/// two suppliers and the repositories the use cases depend on.
/// </summary>
internal sealed class SupplierQuotationTestContext
{
    public static readonly DateTimeOffset IssuedAt = new(2026, 8, 4, 15, 30, 0, TimeSpan.Zero);
    public static readonly DateOnly Deadline = new(2026, 8, 10);

    /// <summary>End of the deadline day in Brasília (UTC-3).</summary>
    public static readonly DateTimeOffset Cutoff = new(2026, 8, 11, 3, 0, 0, TimeSpan.Zero);

    public SupplierQuotationTestContext()
    {
        List = ShoppingListEntity.Create(UserId, "Reposição da semana");
        Rice = ShoppingItemEntity.Create(List.Id, "Arroz 5 kg", 10, "un");
        Beans = ShoppingItemEntity.Create(List.Id, "Feijão 1 kg", 20, "un");
        SupplierA = SupplierEntity.Create(
            UserId,
            "Atacado Alfa",
            contact: SupplierContact.Create("vendas@alfa.com.br", "11999990000"));
        SupplierB = SupplierEntity.Create(UserId, "Distribuidora Beta");

        Lists[List.Id] = List;
        Items.AddRange([Rice, Beans]);
        Suppliers[SupplierA.Id] = SupplierA;
        Suppliers[SupplierB.Id] = SupplierB;

        CurrentUser.SetupGet(user => user.UserId).Returns(UserId);
        CurrentUser.SetupGet(user => user.Name).Returns("Marina Compradora");
        CurrentUser.SetupGet(user => user.Email).Returns("marina@empresa.com.br");

        FrontendUrl
            .Setup(service => service.BuildSupplierQuotationUrl(It.IsAny<string>()))
            .Returns<string>(token => $"https://app.test/cotacao/{token}");

        ShoppingListRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Lists.GetValueOrDefault(id));
        ShoppingItemRepository
            .Setup(repository => repository.GetByShoppingListIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid listId, CancellationToken _) =>
                Items.Where(item => item.ShoppingListId == listId).ToList());
        SupplierRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Suppliers.GetValueOrDefault(id));

        LinkRepository
            .Setup(repository => repository.ExistsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid listId, Guid supplierId, CancellationToken _) =>
                Links.Any(link => link.ShoppingListId == listId && link.SupplierId == supplierId));
        LinkRepository
            .Setup(repository => repository.GetLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid listId, Guid supplierId, CancellationToken _) =>
                Links.FirstOrDefault(link => link.ShoppingListId == listId && link.SupplierId == supplierId));
        LinkRepository
            .Setup(repository => repository.GetLinksAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid listId, CancellationToken _) =>
                Links.Where(link => link.ShoppingListId == listId).ToList());
        LinkRepository
            .Setup(repository => repository.AddAsync(It.IsAny<ShoppingListSupplierEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ShoppingListSupplierEntity, CancellationToken>((link, _) => Links.Add(link))
            .Returns(Task.CompletedTask);

        ItemQuoteRepository
            .Setup(repository => repository.GetBySupplierInShoppingListAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid listId, Guid supplierId, CancellationToken _) =>
                Quotes
                    .Where(quote => quote.SupplierId == supplierId
                        && Items.Any(item => item.Id == quote.ShoppingItemId && item.ShoppingListId == listId))
                    .ToList());
        ItemQuoteRepository
            .Setup(repository => repository.AddAsync(It.IsAny<ItemQuoteEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ItemQuoteEntity, CancellationToken>((quote, _) => Quotes.Add(quote))
            .Returns(Task.CompletedTask);
        ItemQuoteRepository
            .Setup(repository => repository.Remove(It.IsAny<ItemQuoteEntity>()))
            .Callback<ItemQuoteEntity>(quote => Quotes.Remove(quote));
    }

    public Guid UserId { get; } = Guid.NewGuid();
    public ShoppingListEntity List { get; }
    public ShoppingItemEntity Rice { get; }
    public ShoppingItemEntity Beans { get; }
    public SupplierEntity SupplierA { get; }
    public SupplierEntity SupplierB { get; }

    public Dictionary<Guid, ShoppingListEntity> Lists { get; } = [];
    public List<ShoppingItemEntity> Items { get; } = [];
    public Dictionary<Guid, SupplierEntity> Suppliers { get; } = [];
    public List<ShoppingListSupplierEntity> Links { get; } = [];
    public List<ItemQuoteEntity> Quotes { get; } = [];

    public MutableTimeProvider Clock { get; } = new(IssuedAt);
    public InMemoryQuotationRequestRepository Requests { get; } = new();
    public Mock<ICurrentUser> CurrentUser { get; } = new();
    public Mock<IFrontendUrlService> FrontendUrl { get; } = new();
    public Mock<IShoppingListRepository> ShoppingListRepository { get; } = new();
    public Mock<IShoppingItemRepository> ShoppingItemRepository { get; } = new();
    public Mock<ISupplierRepository> SupplierRepository { get; } = new();
    public Mock<IShoppingListSupplierRepository> LinkRepository { get; } = new();
    public Mock<IItemQuoteRepository> ItemQuoteRepository { get; } = new();
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Mock<IEmailAvailability> EmailAvailability { get; } = new();
    public Mock<IEmailService> EmailService { get; } = new();
    public int EmailServiceResolutions { get; private set; }

    public SupplierQuotationEmailSender EmailSender =>
        new(
            EmailAvailability.Object,
            new Lazy<IEmailService>(() =>
            {
                EmailServiceResolutions++;
                return EmailService.Object;
            }));

    public PublicSupplierQuotationResolver Resolver => new(Requests, Clock);

    public GetPublicSupplierQuotationUseCase GetPublicUseCase => new(Resolver, UnitOfWork.Object);

    public SubmitPublicSupplierQuotationUseCase SubmitUseCase =>
        new(
            Resolver,
            ShoppingListRepository.Object,
            ShoppingItemRepository.Object,
            SupplierRepository.Object,
            LinkRepository.Object,
            ItemQuoteRepository.Object,
            UnitOfWork.Object);

    /// <summary>
    /// Issues a request for both suppliers and returns the raw token of each one, the way
    /// the buyer receives them (inside the link) when the request is created.
    /// </summary>
    public IssuedRequest IssueRequest(DateOnly? deadline = null, bool withoutDeadline = false)
    {
        var request = QuotationRequestEntity.Create(
            UserId,
            List.Id,
            List.Name,
            null,
            "Marina Compradora",
            "marina@empresa.com.br",
            withoutDeadline ? null : deadline ?? Deadline,
            "Rua A, 10",
            "Informar marca.",
            [
                new QuotationRequestItemSnapshot(Rice.Id, Rice.Name, Rice.Quantity, Rice.Unit),
                new QuotationRequestItemSnapshot(Beans.Id, Beans.Name, Beans.Quantity, Beans.Unit)
            ],
            Clock.GetUtcNow().UtcDateTime);

        var (tokenA, hashA) = Application.Services.QuotationLinkToken.Create();
        var (tokenB, hashB) = Application.Services.QuotationLinkToken.Create();
        var quotationA = request.AddSupplierQuotation(
            new SupplierQuotationRecipient(SupplierA.Id, SupplierA.Name, "vendas@alfa.com.br", "11999990000", hashA),
            Clock.GetUtcNow().UtcDateTime);
        var quotationB = request.AddSupplierQuotation(
            new SupplierQuotationRecipient(SupplierB.Id, SupplierB.Name, null, null, hashB),
            Clock.GetUtcNow().UtcDateTime);
        Requests.Stored.Add(request);

        var items = request.Items.OrderBy(item => item.Position).ToList();
        return new IssuedRequest(request, quotationA, tokenA, quotationB, tokenB, items[0], items[1]);
    }
}

internal sealed record IssuedRequest(
    QuotationRequestEntity Request,
    SupplierQuotation QuotationA,
    string TokenA,
    SupplierQuotation QuotationB,
    string TokenB,
    QuotationRequestItem RiceItem,
    QuotationRequestItem BeansItem);

internal sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}

internal sealed class InMemoryQuotationRequestRepository : IQuotationRequestRepository
{
    public List<QuotationRequestEntity> Stored { get; } = [];
    public int TokenLookups { get; private set; }

    public Task<QuotationRequestEntity?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Stored.FirstOrDefault(request => request.Id == id && request.UserId == userId));

    public Task<QuotationRequestEntity?> GetTrackedByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        GetByIdForUserAsync(id, userId, cancellationToken);

    public Task<SupplierQuotationAccess?> GetSupplierQuotationByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        TokenLookups++;
        foreach (var request in Stored)
        {
            var quotation = request.SupplierQuotations
                .FirstOrDefault(candidate => candidate.TokenHash == tokenHash);
            if (quotation is not null)
            {
                return Task.FromResult<SupplierQuotationAccess?>(
                    new SupplierQuotationAccess(request, quotation));
            }
        }

        return Task.FromResult<SupplierQuotationAccess?>(null);
    }

    public Task<List<QuotationRequestEntity>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Stored.Where(request => request.UserId == userId).ToList());

    public Task<bool> DeleteForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Stored.RemoveAll(request => request.Id == id && request.UserId == userId) > 0);

    public Task AddAsync(
        QuotationRequestEntity request,
        CancellationToken cancellationToken = default)
    {
        Stored.Add(request);
        return Task.CompletedTask;
    }
}
