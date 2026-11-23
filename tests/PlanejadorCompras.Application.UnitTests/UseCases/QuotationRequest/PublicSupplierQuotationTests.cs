using System.Text.Json;
using Moq;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Rules;
using ItemQuoteEntity = PlanejadorCompras.Domain.Entities.ItemQuote;
using ShoppingListSupplierEntity = PlanejadorCompras.Domain.Entities.ShoppingListSupplier;
using SupplierEntity = PlanejadorCompras.Domain.Entities.Supplier;

namespace PlanejadorCompras.Application.UnitTests.UseCases.QuotationRequest;

/// <summary>Security and business rules of the public link a supplier uses without login.</summary>
public sealed class PublicSupplierQuotationTests
{
    private readonly SupplierQuotationTestContext _context = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("../../../../etc/passwd-padding-to-43-characters")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")]
    public async Task Get_ShouldAnswerNotFoundWithoutTouchingTheDatabase_WhenTokenIsMalformed(
        string? token)
    {
        _context.IssueRequest();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _context.GetPublicUseCase.ExecuteAsync(token));

        Assert.Equal("quotation_link_not_found", exception.ErrorCode);
        Assert.Equal(0, _context.Requests.TokenLookups);
    }

    [Fact]
    public async Task Get_ShouldAnswerNotFound_WhenTokenIsWellFormedButUnknown()
    {
        _context.IssueRequest();
        var (unknownToken, _) = QuotationLinkToken.Create();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _context.GetPublicUseCase.ExecuteAsync(unknownToken));

        Assert.Equal("quotation_link_not_found", exception.ErrorCode);
    }

    [Fact]
    public async Task Get_ShouldNotAcceptTheStoredHashAsToken()
    {
        var issued = _context.IssueRequest();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _context.GetPublicUseCase.ExecuteAsync(issued.QuotationA.TokenHash));
        Assert.NotEqual(issued.TokenA, issued.QuotationA.TokenHash);
    }

    [Fact]
    public async Task Get_ShouldRejectAnExpiredLink()
    {
        var issued = _context.IssueRequest();
        _context.Clock.UtcNow = new DateTimeOffset(
            issued.QuotationA.TokenExpiresAtUtc.AddSeconds(1),
            TimeSpan.Zero);

        var exception = await Assert.ThrowsAsync<GoneException>(() =>
            _context.GetPublicUseCase.ExecuteAsync(issued.TokenA));

        Assert.Equal("quotation_link_expired", exception.ErrorCode);
        Assert.Null(issued.QuotationA.ViewedAtUtc);
    }

    [Fact]
    public async Task GetAndSubmit_ShouldRejectARevokedLink()
    {
        var issued = _context.IssueRequest();
        issued.QuotationA.Revoke(_context.Clock.UtcNow.UtcDateTime);

        var readError = await Assert.ThrowsAsync<GoneException>(() =>
            _context.GetPublicUseCase.ExecuteAsync(issued.TokenA));
        var writeError = await Assert.ThrowsAsync<GoneException>(() =>
            _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued)));

        Assert.Equal("quotation_link_revoked", readError.ErrorCode);
        Assert.Equal("quotation_link_revoked", writeError.ErrorCode);
        Assert.Empty(issued.QuotationA.ResponseItems);
        Assert.Empty(_context.Quotes);
        // Revoking one supplier does not affect the other.
        await _context.GetPublicUseCase.ExecuteAsync(issued.TokenB);
    }

    [Fact]
    public async Task Get_ShouldStopAcceptingTheOldToken_WhenTheLinkIsReplaced()
    {
        var issued = _context.IssueRequest();
        var (newToken, newHash) = QuotationLinkToken.Create();
        var now = _context.Clock.UtcNow.UtcDateTime;

        issued.QuotationA.RotateToken(newHash, issued.Request.CalculateLinkExpiration(now), now);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _context.GetPublicUseCase.ExecuteAsync(issued.TokenA));
        var result = await _context.GetPublicUseCase.ExecuteAsync(newToken);
        Assert.Equal("Atacado Alfa", result.SupplierName);
    }

    [Fact]
    public async Task Get_ShouldMarkTheQuotationAsViewedOnlyOnTheFirstOpening()
    {
        var issued = _context.IssueRequest();
        var firstOpening = _context.Clock.UtcNow.UtcDateTime;

        await _context.GetPublicUseCase.ExecuteAsync(issued.TokenA);
        _context.Clock.UtcNow = _context.Clock.UtcNow.AddHours(2);
        await _context.GetPublicUseCase.ExecuteAsync(issued.TokenA);

        Assert.Equal(SupplierQuotationStatus.Viewed, issued.QuotationA.Status);
        Assert.Equal(firstOpening, issued.QuotationA.ViewedAtUtc);
        Assert.Equal(SupplierQuotationStatus.Sent, issued.QuotationB.Status);
        _context.UnitOfWork.Verify(
            unit => unit.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Get_ShouldExposeOnlyWhatTheSupplierNeeds()
    {
        var issued = _context.IssueRequest();
        await _context.SubmitUseCase.ExecuteAsync(
            issued.TokenB,
            new SubmitSupplierQuotationRequestDto(
                [new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 4321.09m)],
                PaymentTerms: "Somente Beta sabe"));

        var result = await _context.GetPublicUseCase.ExecuteAsync(issued.TokenA);
        var json = JsonSerializer.Serialize(result);

        Assert.Equal("Marina Compradora", result.BuyerName);
        Assert.Equal("Atacado Alfa", result.SupplierName);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.Null(item.UnitPrice));
        Assert.Null(result.Terms);
        Assert.Null(result.RespondedAtUtc);
        // Nothing from the other supplier, no buyer e-mail and no internal ids.
        Assert.DoesNotContain("Beta", json);
        Assert.DoesNotContain("4321.09", json);
        Assert.DoesNotContain("marina@empresa.com.br", json);
        Assert.DoesNotContain(_context.UserId.ToString(), json);
        Assert.DoesNotContain(_context.List.Id.ToString(), json);
        Assert.DoesNotContain(_context.SupplierA.Id.ToString(), json);
        Assert.DoesNotContain(_context.SupplierB.Id.ToString(), json);
        Assert.DoesNotContain(issued.QuotationA.TokenHash, json);
        Assert.DoesNotContain(_context.Rice.Id.ToString(), json);
    }

    [Fact]
    public async Task Submit_ShouldChangeOnlyTheQuotationOfTheTokenOwner()
    {
        var issued = _context.IssueRequest();

        await _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued));

        Assert.Equal(SupplierQuotationStatus.Responded, issued.QuotationA.Status);
        Assert.Equal(SupplierQuotationStatus.Sent, issued.QuotationB.Status);
        Assert.Empty(issued.QuotationB.ResponseItems);
        Assert.True(issued.QuotationB.Terms.IsEmpty);
        Assert.All(_context.Quotes, quote => Assert.Equal(_context.SupplierA.Id, quote.SupplierId));
        Assert.DoesNotContain(_context.Links, link => link.SupplierId == _context.SupplierB.Id);
    }

    [Fact]
    public async Task Submit_ShouldSendPricesAndTermsToThePriceMapAndMarkAsResponded()
    {
        var issued = _context.IssueRequest();
        _context.Links.Add(ShoppingListSupplierEntity.Create(_context.List.Id, _context.SupplierA.Id));
        var typedByBuyer = ItemQuoteEntity.Create(_context.Rice.Id, _context.SupplierA.Id, 30m);
        _context.Quotes.Add(typedByBuyer);
        _context.Clock.UtcNow = _context.Clock.UtcNow.AddDays(1);

        var result = await _context.SubmitUseCase.ExecuteAsync(
            issued.TokenA,
            new SubmitSupplierQuotationRequestDto(
                [
                    new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 25.50m),
                    new SubmitSupplierQuotationPriceDto(issued.BeansItem.Id, 8.90m)
                ],
                DeliveryDays: 3,
                PaymentTerms: "  28 dias  ",
                FreightCost: 45m,
                MinimumOrderValue: 200m,
                ProposalValidUntil: new DateOnly(2026, 8, 20),
                Notes: "Entrega pela manhã."));

        Assert.NotNull(result.RespondedAtUtc);
        Assert.Equal(_context.Clock.UtcNow.UtcDateTime, issued.QuotationA.RespondedAtUtc);
        Assert.NotNull(issued.QuotationA.ViewedAtUtc);
        Assert.Equal(25.50m, result.Items.Single(item => item.Id == issued.RiceItem.Id).UnitPrice);
        Assert.Equal(2, _context.Quotes.Count);
        Assert.Same(typedByBuyer, _context.Quotes.Single(quote => quote.ShoppingItemId == _context.Rice.Id));
        Assert.Equal(25.50m, typedByBuyer.UnitPrice);
        Assert.Equal(8.90m, _context.Quotes.Single(quote => quote.ShoppingItemId == _context.Beans.Id).UnitPrice);
        var link = Assert.Single(_context.Links);
        Assert.Equal(45m, link.Terms.FreightCost);
        Assert.Equal("28 dias", link.Terms.PaymentTerms);
        Assert.Equal(3, link.Terms.DeliveryDays);
        Assert.Equal(200m, link.Terms.MinimumOrderValue);
        Assert.Equal(_context.Clock.UtcNow.UtcDateTime, link.TermsUpdatedAtUtc);
        _context.UnitOfWork.Verify(
            unit => unit.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Submit_ShouldLinkTheSupplierToTheList_WhenTheBuyerRemovedTheLink()
    {
        var issued = _context.IssueRequest();

        await _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued));

        var link = Assert.Single(_context.Links);
        Assert.Equal(_context.SupplierA.Id, link.SupplierId);
        Assert.Equal(_context.List.Id, link.ShoppingListId);
    }

    [Fact]
    public async Task Submit_ShouldReplaceThePreviousAnswerAndWithdrawPricesLeftBlank_WhileBeforeTheDeadline()
    {
        var issued = _context.IssueRequest();
        await _context.SubmitUseCase.ExecuteAsync(
            issued.TokenA,
            new SubmitSupplierQuotationRequestDto(
                [
                    new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 25m),
                    new SubmitSupplierQuotationPriceDto(issued.BeansItem.Id, 9m)
                ],
                FreightCost: 40m));
        _context.Clock.UtcNow = SupplierQuotationTestContext.Cutoff.AddSeconds(-1);

        await _context.SubmitUseCase.ExecuteAsync(
            issued.TokenA,
            new SubmitSupplierQuotationRequestDto(
                [new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 24m)]));

        var responseItem = Assert.Single(issued.QuotationA.ResponseItems);
        Assert.Equal(24m, responseItem.UnitPrice);
        var quote = Assert.Single(_context.Quotes);
        Assert.Equal(_context.Rice.Id, quote.ShoppingItemId);
        Assert.Equal(24m, quote.UnitPrice);
        Assert.Null(issued.QuotationA.Terms.FreightCost);
        Assert.Null(Assert.Single(_context.Links).Terms.FreightCost);
    }

    [Fact]
    public async Task Submit_ShouldKeepAPriceTypedByTheBuyer_WhenTheSupplierNeverAnsweredThatItem()
    {
        var issued = _context.IssueRequest();
        var typedByBuyer = ItemQuoteEntity.Create(_context.Beans.Id, _context.SupplierA.Id, 9.99m);
        _context.Quotes.Add(typedByBuyer);

        await _context.SubmitUseCase.ExecuteAsync(
            issued.TokenA,
            new SubmitSupplierQuotationRequestDto(
                [new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 24m)]));

        Assert.Contains(typedByBuyer, _context.Quotes);
        Assert.Equal(9.99m, typedByBuyer.UnitPrice);
    }

    [Fact]
    public async Task Submit_ShouldRejectAnAnswerAfterTheDeadline_AndKeepThePreviousOne()
    {
        var issued = _context.IssueRequest();
        await _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued));
        var answeredAt = issued.QuotationA.RespondedAtUtc;
        _context.Clock.UtcNow = SupplierQuotationTestContext.Cutoff;

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _context.SubmitUseCase.ExecuteAsync(
                issued.TokenA,
                new SubmitSupplierQuotationRequestDto(
                    [new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 1m)])));

        Assert.Equal("quotation_response_deadline_passed", exception.ErrorCode);
        Assert.Equal(answeredAt, issued.QuotationA.RespondedAtUtc);
        Assert.Equal(2, issued.QuotationA.ResponseItems.Count);
        Assert.DoesNotContain(_context.Quotes, quote => quote.UnitPrice == 1m);
        // The link still opens for reading until it expires, but tells that it is closed.
        var readOnly = await _context.GetPublicUseCase.ExecuteAsync(issued.TokenA);
        Assert.False(readOnly.CanRespond);
        Assert.NotNull(readOnly.RespondedAtUtc);
    }

    [Fact]
    public async Task Submit_ShouldRejectAFirstAnswerAfterTheDeadline()
    {
        var issued = _context.IssueRequest();
        _context.Clock.UtcNow = SupplierQuotationTestContext.Cutoff.AddHours(1);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued)));

        Assert.Equal(SupplierQuotationStatus.Sent, issued.QuotationA.Status);
        Assert.Empty(_context.Quotes);
    }

    [Fact]
    public async Task Submit_ShouldRejectItemsThatAreNotPartOfTheQuotation()
    {
        var issued = _context.IssueRequest();
        var otherRequest = _context.IssueRequest();

        var foreignItem = await Assert.ThrowsAsync<BadRequestException>(() =>
            _context.SubmitUseCase.ExecuteAsync(
                issued.TokenA,
                new SubmitSupplierQuotationRequestDto(
                    [new SubmitSupplierQuotationPriceDto(otherRequest.RiceItem.Id, 10m)])));
        var duplicatedItem = await Assert.ThrowsAsync<BadRequestException>(() =>
            _context.SubmitUseCase.ExecuteAsync(
                issued.TokenA,
                new SubmitSupplierQuotationRequestDto(
                    [
                        new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 10m),
                        new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 11m)
                    ])));
        // The id of the live shopping item is internal and must not be accepted either.
        var internalId = await Assert.ThrowsAsync<BadRequestException>(() =>
            _context.SubmitUseCase.ExecuteAsync(
                issued.TokenA,
                new SubmitSupplierQuotationRequestDto(
                    [new SubmitSupplierQuotationPriceDto(_context.Rice.Id, 10m)])));

        Assert.Equal("quotation_response_invalid_item", foreignItem.ErrorCode);
        Assert.Equal("quotation_response_invalid_item", duplicatedItem.ErrorCode);
        Assert.Equal("quotation_response_invalid_item", internalId.ErrorCode);
        Assert.Empty(issued.QuotationA.ResponseItems);
        Assert.Empty(_context.Quotes);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("0.001")]
    [InlineData("10.999")]
    [InlineData("1000000000")]
    public async Task Submit_ShouldRejectInvalidPrices(string unitPrice)
    {
        var issued = _context.IssueRequest();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _context.SubmitUseCase.ExecuteAsync(
                issued.TokenA,
                new SubmitSupplierQuotationRequestDto(
                    [new SubmitSupplierQuotationPriceDto(
                        issued.RiceItem.Id,
                        decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture))])));

        Assert.Equal("quotation_response_invalid_price", exception.ErrorCode);
        Assert.Empty(_context.Quotes);
    }

    [Fact]
    public async Task Submit_ShouldRejectAnAnswerWithoutPrices()
    {
        var issued = _context.IssueRequest();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _context.SubmitUseCase.ExecuteAsync(
                issued.TokenA,
                new SubmitSupplierQuotationRequestDto([])));

        Assert.Equal("quotation_response_without_prices", exception.ErrorCode);
    }

    [Fact]
    public async Task Submit_ShouldRejectInvalidCommercialTerms()
    {
        var issued = _context.IssueRequest();
        SubmitSupplierQuotationPriceDto[] prices =
            [new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 10m)];
        SubmitSupplierQuotationRequestDto[] invalidRequests =
        [
            new(prices, FreightCost: -0.01m),
            new(prices, FreightCost: 10.001m),
            new(prices, MinimumOrderValue: -5m),
            new(prices, MinimumOrderValue: CommercialTermsRules.MaximumAmount + 1),
            new(prices, DeliveryDays: -1),
            new(prices, DeliveryDays: CommercialTermsRules.MaximumDeliveryDays + 1),
            new(prices, PaymentTerms: new string('x', CommercialTermsRules.PaymentTermsMaxLength + 1)),
            new(prices, Notes: new string('x', CommercialTermsRules.NotesMaxLength + 1)),
            new(prices, ProposalValidUntil: new DateOnly(2026, 8, 3)),
            new(prices, ProposalValidUntil: new DateOnly(2040, 1, 1))
        ];

        foreach (var request in invalidRequests)
        {
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                _context.SubmitUseCase.ExecuteAsync(issued.TokenA, request));
            Assert.Equal("quotation_response_invalid_terms", exception.ErrorCode);
        }

        Assert.Equal(SupplierQuotationStatus.Sent, issued.QuotationA.Status);
        Assert.Empty(_context.Quotes);
        _context.UnitOfWork.Verify(
            unit => unit.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Submit_ShouldKeepTheAnswerButNotTouchThePriceMap_WhenTheListWasDeleted()
    {
        var issued = _context.IssueRequest();
        _context.Lists.Clear();

        await _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued));

        Assert.Equal(SupplierQuotationStatus.Responded, issued.QuotationA.Status);
        Assert.Empty(_context.Quotes);
        Assert.Empty(_context.Links);
    }

    [Fact]
    public async Task Submit_ShouldNotWriteIntoAListOrSupplierOfAnotherOwner()
    {
        var issued = _context.IssueRequest();
        // Same ids, but the supplier now belongs to somebody else.
        _context.Suppliers[_context.SupplierA.Id] = SupplierEntity.Create(Guid.NewGuid(), "Atacado Alfa");

        await _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued));

        Assert.Equal(SupplierQuotationStatus.Responded, issued.QuotationA.Status);
        Assert.Empty(_context.Quotes);
        Assert.Empty(_context.Links);
    }

    [Fact]
    public async Task Submit_ShouldSkipItemsRemovedFromTheList()
    {
        var issued = _context.IssueRequest();
        _context.Items.Remove(_context.Beans);

        await _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued));

        var quote = Assert.Single(_context.Quotes);
        Assert.Equal(_context.Rice.Id, quote.ShoppingItemId);
        Assert.Equal(2, issued.QuotationA.ResponseItems.Count);
    }

    [Fact]
    public async Task Submit_ShouldBeOpenUntilTheLinkExpires_WhenTheRequestHasNoDeadline()
    {
        var issued = _context.IssueRequest(withoutDeadline: true);
        Assert.Equal(
            SupplierQuotationTestContext.IssuedAt.UtcDateTime + SupplierQuotationRules.LinkLifetimeWithoutDeadline,
            issued.QuotationA.TokenExpiresAtUtc);
        _context.Clock.UtcNow = SupplierQuotationTestContext.IssuedAt.AddDays(29);

        await _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued));
        _context.Clock.UtcNow = SupplierQuotationTestContext.IssuedAt.AddDays(31);
        var exception = await Assert.ThrowsAsync<GoneException>(() =>
            _context.SubmitUseCase.ExecuteAsync(issued.TokenA, ValidResponse(issued)));

        Assert.Equal("quotation_link_expired", exception.ErrorCode);
    }

    private static SubmitSupplierQuotationRequestDto ValidResponse(IssuedRequest issued) =>
        new(
            [
                new SubmitSupplierQuotationPriceDto(issued.RiceItem.Id, 25.50m),
                new SubmitSupplierQuotationPriceDto(issued.BeansItem.Id, 8.90m)
            ],
            FreightCost: 45m);
}
