using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Features.ShoppingLists.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.QuotationRequest;
using PlanejadorCompras.Application.UseCases.ShoppingList;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Rules;
using ShoppingListSupplierEntity = PlanejadorCompras.Domain.Entities.ShoppingListSupplier;
using SupplierEntity = PlanejadorCompras.Domain.Entities.Supplier;

namespace PlanejadorCompras.Application.UnitTests.UseCases.QuotationRequest;

/// <summary>Buyer side: issuing one quotation per supplier, links, e-mail and terms.</summary>
public sealed class SupplierQuotationBuyerTests
{
    private readonly SupplierQuotationTestContext _context = new();
    private readonly Mock<IShoppingListDetailQuery> _detailQuery = new();
    private readonly Mock<IShoppingListAccessService> _listAccess = new();
    private readonly Mock<ISupplierAccessService> _supplierAccess = new();

    public SupplierQuotationBuyerTests()
    {
        _detailQuery
            .Setup(query => query.GetByIdAsync(_context.UserId, _context.List.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShoppingListDetailResponseDto(
                _context.List.Id,
                _context.List.Name,
                null,
                _context.List.CreatedAt,
                2,
                0,
                0,
                [
                    new ShoppingListDetailItemDto(_context.Rice.Id, "Arroz 5 kg", 10, "un", _context.Rice.CreatedAt, 0, null),
                    new ShoppingListDetailItemDto(_context.Beans.Id, "Feijão 1 kg", 20, "un", _context.Beans.CreatedAt, 0, null)
                ]));
        _listAccess
            .Setup(service => service.GetForCurrentUserAsync(_context.List.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_context.List);
        _supplierAccess
            .Setup(service => service.GetForCurrentUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => _context.Suppliers[id]);
    }

    [Fact]
    public async Task Create_ShouldIssueOneQuotationPerSupplier_WithItsOwnLinkAndOnlyTheHashStored()
    {
        var result = await CreateUseCase().ExecuteAsync(
            _context.List.Id,
            new QuotationRequestPdfRequestDto(
                SupplierQuotationTestContext.Deadline,
                SupplierIds: [_context.SupplierA.Id, _context.SupplierB.Id, _context.SupplierA.Id]));

        var persisted = Assert.Single(_context.Requests.Stored);
        Assert.Equal(2, persisted.SupplierQuotations.Count);
        Assert.Equal(2, result.Suppliers.Count);
        Assert.Equal(2, result.Suppliers.Select(supplier => supplier.LinkUrl).Distinct().Count());
        foreach (var supplier in result.Suppliers)
        {
            var quotation = persisted.SupplierQuotations.Single(candidate => candidate.Id == supplier.Id);
            var token = supplier.LinkUrl!.Split('/').Last();
            Assert.Equal(QuotationLinkToken.TokenLength, token.Length);
            Assert.Equal(QuotationLinkToken.Hash(token), quotation.TokenHash);
            Assert.NotEqual(token, quotation.TokenHash);
            Assert.Equal("sent", supplier.Status);
            Assert.True(supplier.IsLinkActive);
            Assert.Null(supplier.EmailDelivery);
            // Read-only for 7 days after the end of the deadline day (Brasília time).
            Assert.Equal(
                SupplierQuotationTestContext.Cutoff.UtcDateTime + SupplierQuotationRules.ReadOnlyPeriodAfterDeadline,
                quotation.TokenExpiresAtUtc);
        }

        var alfa = result.Suppliers.Single(supplier => supplier.SupplierName == "Atacado Alfa");
        Assert.Equal("vendas@alfa.com.br", alfa.SupplierEmail);
        Assert.Equal(2, _context.Links.Count);
        Assert.Equal(0, _context.EmailServiceResolutions);
    }

    [Fact]
    public async Task Create_ShouldKeepWorkingWithoutSuppliers()
    {
        var result = await CreateUseCase().ExecuteAsync(
            _context.List.Id,
            new QuotationRequestPdfRequestDto());

        Assert.Empty(result.Suppliers);
        Assert.Empty(Assert.Single(_context.Requests.Stored).SupplierQuotations);
    }

    [Fact]
    public async Task Create_ShouldHideASupplierOfAnotherUser()
    {
        var foreignSupplier = SupplierEntity.Create(Guid.NewGuid(), "Fornecedor alheio");
        _context.Suppliers[foreignSupplier.Id] = foreignSupplier;

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateUseCase().ExecuteAsync(
                _context.List.Id,
                new QuotationRequestPdfRequestDto(SupplierIds: [_context.SupplierA.Id, foreignSupplier.Id])));

        Assert.Equal("supplier_not_found", exception.ErrorCode);
        Assert.Empty(_context.Requests.Stored);
        Assert.Empty(_context.Links);
    }

    [Fact]
    public async Task Create_ShouldRefuseEmailWithAClearError_WhenTheProviderIsNotConfigured()
    {
        _context.EmailAvailability.SetupGet(email => email.IsAvailable).Returns(false);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateUseCase().ExecuteAsync(
                _context.List.Id,
                new QuotationRequestPdfRequestDto(SupplierIds: [_context.SupplierA.Id], SendEmail: true)));

        Assert.Equal("email_delivery_unavailable", exception.ErrorCode);
        Assert.Empty(_context.Requests.Stored);
        // The e-mail client throws when built without a key: it must never be resolved here.
        Assert.Equal(0, _context.EmailServiceResolutions);
    }

    [Fact]
    public async Task Create_ShouldSendTheLinkByEmailAndReportEachDelivery()
    {
        _context.EmailAvailability.SetupGet(email => email.IsAvailable).Returns(true);
        string? sentTo = null;
        string? sentBody = null;
        _context.EmailService
            .Setup(email => email.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((to, _, body, _) =>
            {
                sentTo = to;
                sentBody = body;
            })
            .Returns(Task.CompletedTask);

        var result = await CreateUseCase().ExecuteAsync(
            _context.List.Id,
            new QuotationRequestPdfRequestDto(
                SupplierIds: [_context.SupplierA.Id, _context.SupplierB.Id],
                SendEmail: true));

        var alfa = result.Suppliers.Single(supplier => supplier.SupplierName == "Atacado Alfa");
        var beta = result.Suppliers.Single(supplier => supplier.SupplierName == "Distribuidora Beta");
        Assert.Equal("sent", alfa.EmailDelivery);
        Assert.NotNull(alfa.EmailSentAtUtc);
        Assert.Equal("no_address", beta.EmailDelivery);
        Assert.Null(beta.EmailSentAtUtc);
        Assert.Equal("vendas@alfa.com.br", sentTo);
        Assert.Contains(alfa.LinkUrl!, sentBody);
        Assert.DoesNotContain(beta.LinkUrl!, sentBody);
        _context.EmailService.Verify(
            email => email.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_ShouldKeepTheRequest_WhenTheEmailProviderFails()
    {
        _context.EmailAvailability.SetupGet(email => email.IsAvailable).Returns(true);
        _context.EmailService
            .Setup(email => email.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider down"));

        var result = await CreateUseCase().ExecuteAsync(
            _context.List.Id,
            new QuotationRequestPdfRequestDto(SupplierIds: [_context.SupplierA.Id], SendEmail: true));

        var supplier = Assert.Single(result.Suppliers);
        Assert.Equal("failed", supplier.EmailDelivery);
        Assert.Null(supplier.EmailSentAtUtc);
        Assert.NotNull(supplier.LinkUrl);
        Assert.Single(_context.Requests.Stored);
    }

    [Fact]
    public async Task Email_ShouldEncodeTextTypedByUsers()
    {
        _context.EmailAvailability.SetupGet(email => email.IsAvailable).Returns(true);
        _context.CurrentUser.SetupGet(user => user.Name).Returns("<script>alert(1)</script>\r\nBcc: x@y.z");
        string? subject = null;
        string? body = null;
        _context.EmailService
            .Setup(email => email.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, sentSubject, sentBody, _) =>
            {
                subject = sentSubject;
                body = sentBody;
            })
            .Returns(Task.CompletedTask);

        await CreateUseCase().ExecuteAsync(
            _context.List.Id,
            new QuotationRequestPdfRequestDto(SupplierIds: [_context.SupplierA.Id], SendEmail: true));

        Assert.NotNull(body);
        Assert.DoesNotContain("<script>", body);
        Assert.Contains("&lt;script&gt;", body);
        Assert.DoesNotContain('\n', subject!);
        Assert.DoesNotContain('\r', subject!);
    }

    [Fact]
    public async Task RotateLink_ShouldIssueANewLinkAndReactivateARevokedOne()
    {
        var issued = _context.IssueRequest();
        issued.QuotationA.Revoke(_context.Clock.UtcNow.UtcDateTime);
        var previousHash = issued.QuotationA.TokenHash;

        var result = await RotateUseCase().ExecuteAsync(
            issued.Request.Id,
            issued.QuotationA.Id,
            new SupplierQuotationLinkRequestDto());

        var newToken = result.LinkUrl!.Split('/').Last();
        Assert.NotEqual(issued.TokenA, newToken);
        Assert.NotEqual(previousHash, issued.QuotationA.TokenHash);
        Assert.Equal(QuotationLinkToken.Hash(newToken), issued.QuotationA.TokenHash);
        Assert.False(result.IsLinkRevoked);
        Assert.True(result.IsLinkActive);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _context.GetPublicUseCase.ExecuteAsync(issued.TokenA));
        await _context.GetPublicUseCase.ExecuteAsync(newToken);
    }

    [Fact]
    public async Task RotateLink_ShouldRefuseEmail_WhenTheSupplierHasNoAddress()
    {
        _context.EmailAvailability.SetupGet(email => email.IsAvailable).Returns(true);
        var issued = _context.IssueRequest();
        var previousHash = issued.QuotationB.TokenHash;

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            RotateUseCase().ExecuteAsync(
                issued.Request.Id,
                issued.QuotationB.Id,
                new SupplierQuotationLinkRequestDto(SendEmail: true)));

        Assert.Equal("supplier_without_email", exception.ErrorCode);
        Assert.Equal(previousHash, issued.QuotationB.TokenHash);
    }

    [Fact]
    public async Task RotateAndRevoke_ShouldHideRequestsOfOtherUsers()
    {
        var issued = _context.IssueRequest();
        _context.CurrentUser.SetupGet(user => user.UserId).Returns(Guid.NewGuid());

        var rotate = await Assert.ThrowsAsync<NotFoundException>(() =>
            RotateUseCase().ExecuteAsync(
                issued.Request.Id,
                issued.QuotationA.Id,
                new SupplierQuotationLinkRequestDto()));
        var revoke = await Assert.ThrowsAsync<NotFoundException>(() =>
            RevokeUseCase().ExecuteAsync(issued.Request.Id, issued.QuotationA.Id));

        Assert.Equal("quotation_request_not_found", rotate.ErrorCode);
        Assert.Equal("quotation_request_not_found", revoke.ErrorCode);
        Assert.False(issued.QuotationA.IsRevoked);
    }

    [Fact]
    public async Task Revoke_ShouldCloseTheLinkOfOneSupplierOnly()
    {
        var issued = _context.IssueRequest();

        var result = await RevokeUseCase().ExecuteAsync(issued.Request.Id, issued.QuotationA.Id);

        Assert.True(result.IsLinkRevoked);
        Assert.False(result.IsLinkActive);
        Assert.True(issued.QuotationA.IsRevoked);
        Assert.False(issued.QuotationB.IsRevoked);
    }

    [Fact]
    public async Task GetAll_ShouldShowTheStatusOfEachSupplier()
    {
        var issued = _context.IssueRequest();
        await _context.GetPublicUseCase.ExecuteAsync(issued.TokenB);

        var summary = Assert.Single(await new GetQuotationRequestsUseCase(
            _context.Requests,
            _context.CurrentUser.Object,
            _context.Clock).ExecuteAsync());

        Assert.Equal(2, summary.Suppliers.Count);
        Assert.Equal("sent", summary.Suppliers.Single(supplier => supplier.SupplierName == "Atacado Alfa").Status);
        var beta = summary.Suppliers.Single(supplier => supplier.SupplierName == "Distribuidora Beta");
        Assert.Equal("viewed", beta.Status);
        Assert.NotNull(beta.ViewedAtUtc);
    }

    [Fact]
    public async Task UpdateTerms_ShouldStoreTheTermsTypedByTheBuyer()
    {
        _context.Links.Add(ShoppingListSupplierEntity.Create(_context.List.Id, _context.SupplierA.Id));

        var result = await new UpdateShoppingListSupplierTermsUseCase(
            _context.LinkRepository.Object,
            _listAccess.Object,
            _supplierAccess.Object,
            _context.UnitOfWork.Object,
            _context.Clock).ExecuteAsync(
                _context.List.Id,
                _context.SupplierA.Id,
                new CommercialTermsRequestDto(DeliveryDays: 5, FreightCost: 0m));
        var all = await new GetShoppingListSupplierTermsUseCase(
            _context.LinkRepository.Object,
            _listAccess.Object).ExecuteAsync(_context.List.Id);

        Assert.Equal(0m, result.Terms!.FreightCost);
        Assert.Equal(5, result.Terms.DeliveryDays);
        Assert.Equal(0m, Assert.Single(all).Terms!.FreightCost);
        _context.UnitOfWork.Verify(unit => unit.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTerms_ShouldRejectASupplierThatIsNotInTheList()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateShoppingListSupplierTermsUseCase(
                _context.LinkRepository.Object,
                _listAccess.Object,
                _supplierAccess.Object,
                _context.UnitOfWork.Object,
                _context.Clock).ExecuteAsync(
                    _context.List.Id,
                    _context.SupplierA.Id,
                    new CommercialTermsRequestDto(FreightCost: 10m)));

        Assert.Equal("shopping_list_supplier_not_found", exception.ErrorCode);
    }

    private CreateQuotationRequestUseCase CreateUseCase() =>
        new(
            new GetShoppingListDetailUseCase(_context.CurrentUser.Object, _detailQuery.Object),
            _context.Requests,
            _context.SupplierRepository.Object,
            _context.LinkRepository.Object,
            _context.FrontendUrl.Object,
            _context.EmailSender,
            _context.CurrentUser.Object,
            _context.UnitOfWork.Object,
            _context.Clock);

    private RotateSupplierQuotationLinkUseCase RotateUseCase() =>
        new(
            _context.Requests,
            _context.SupplierRepository.Object,
            _context.FrontendUrl.Object,
            _context.EmailSender,
            _context.CurrentUser.Object,
            _context.UnitOfWork.Object,
            _context.Clock);

    private RevokeSupplierQuotationLinkUseCase RevokeUseCase() =>
        new(
            _context.Requests,
            _context.CurrentUser.Object,
            _context.UnitOfWork.Object,
            _context.Clock);
}
