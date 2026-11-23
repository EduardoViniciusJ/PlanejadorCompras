using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Rules;
using QuotationRequestEntity = PlanejadorCompras.Domain.Entities.QuotationRequest;

namespace PlanejadorCompras.Application.UnitTests.Domain;

public sealed class SupplierQuotationTests
{
    private static readonly DateTime IssuedAt = new(2026, 8, 4, 15, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Token_ShouldBeLongRandomAndStoredOnlyAsHash()
    {
        var first = QuotationLinkToken.Create();
        var second = QuotationLinkToken.Create();

        Assert.Equal(QuotationLinkToken.TokenLength, first.Token.Length);
        Assert.True(QuotationLinkToken.HasValidFormat(first.Token));
        Assert.NotEqual(first.Token, second.Token);
        Assert.Equal(SupplierQuotationRules.TokenHashLength, first.Hash.Length);
        Assert.Equal(first.Hash, QuotationLinkToken.Hash(first.Token));
        Assert.DoesNotContain(first.Token, first.Hash);
    }

    [Fact]
    public void Deadline_ShouldEndAtMidnightOfBrasilia()
    {
        var request = CreateRequest(new DateOnly(2026, 8, 10));

        Assert.Equal(new DateTime(2026, 8, 11, 3, 0, 0, DateTimeKind.Utc), request.ResponseCutoffUtc);
        Assert.True(request.AcceptsResponsesAt(new DateTime(2026, 8, 11, 2, 59, 59, DateTimeKind.Utc)));
        Assert.False(request.AcceptsResponsesAt(new DateTime(2026, 8, 11, 3, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void LinkExpiration_ShouldNeverBeShorterThanTheReadOnlyPeriod()
    {
        var request = CreateRequest(new DateOnly(2026, 8, 10));
        var longAfterDeadline = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(
            longAfterDeadline + SupplierQuotationRules.ReadOnlyPeriodAfterDeadline,
            request.CalculateLinkExpiration(longAfterDeadline));
        Assert.Null(CreateRequest(null).ResponseCutoffUtc);
        Assert.True(CreateRequest(null).AcceptsResponsesAt(longAfterDeadline));
    }

    [Fact]
    public void Request_ShouldRejectTheSameSupplierTwice()
    {
        var request = CreateRequest(null);
        var supplierId = Guid.NewGuid();
        request.AddSupplierQuotation(Recipient(supplierId), IssuedAt);

        Assert.Throws<InvalidOperationException>(() =>
            request.AddSupplierQuotation(Recipient(supplierId), IssuedAt));
    }

    [Fact]
    public void Respond_ShouldValidatePricesAndReplaceThePreviousAnswer()
    {
        var request = CreateRequest(null);
        var quotation = request.AddSupplierQuotation(Recipient(Guid.NewGuid()), IssuedAt);
        var itemId = request.Items.Single().Id;

        Assert.Throws<ArgumentException>(() =>
            quotation.Respond([], CommercialTerms.Empty(), IssuedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            quotation.Respond([new SupplierQuotationPrice(itemId, -1m)], CommercialTerms.Empty(), IssuedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            quotation.Respond([new SupplierQuotationPrice(itemId, 1.005m)], CommercialTerms.Empty(), IssuedAt));
        Assert.Throws<ArgumentException>(() =>
            quotation.Respond(
                [new SupplierQuotationPrice(itemId, 1m), new SupplierQuotationPrice(itemId, 2m)],
                CommercialTerms.Empty(),
                IssuedAt));

        quotation.Respond([new SupplierQuotationPrice(itemId, 10m)], CommercialTerms.Empty(), IssuedAt);
        quotation.Respond([new SupplierQuotationPrice(itemId, 12m)], CommercialTerms.Empty(), IssuedAt.AddHours(1));

        Assert.Equal(12m, Assert.Single(quotation.ResponseItems).UnitPrice);
        Assert.Equal(SupplierQuotationStatus.Responded, quotation.Status);
        Assert.Equal(IssuedAt.AddHours(1), quotation.RespondedAtUtc);
        Assert.Equal(IssuedAt, quotation.ViewedAtUtc);
    }

    [Theory]
    [InlineData(-1, null, null)]
    [InlineData(366, null, null)]
    [InlineData(null, "-0.01", null)]
    [InlineData(null, "1.234", null)]
    [InlineData(null, null, "1000000000")]
    public void CommercialTerms_ShouldRejectValuesOutOfRange(
        int? deliveryDays,
        string? freightCost,
        string? minimumOrderValue)
    {
        Assert.ThrowsAny<ArgumentException>(() => CommercialTerms.Create(
            deliveryDays,
            null,
            Parse(freightCost),
            Parse(minimumOrderValue),
            null,
            null));
    }

    [Fact]
    public void CommercialTerms_ShouldTrimTextAndTellFreeFreightFromNotInformed()
    {
        var terms = CommercialTerms.Create(0, "  À vista  ", 0m, null, null, "   ");

        Assert.Equal("À vista", terms.PaymentTerms);
        Assert.Equal(0m, terms.FreightCost);
        Assert.Null(terms.Notes);
        Assert.False(terms.IsEmpty);
        Assert.True(CommercialTerms.Create(null, null, null, null, null, null).IsEmpty);
        Assert.Throws<ArgumentException>(() => CommercialTerms.Create(
            null,
            new string('x', CommercialTermsRules.PaymentTermsMaxLength + 1),
            null,
            null,
            null,
            null));
    }

    private static decimal? Parse(string? value) =>
        value is null
            ? null
            : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    private static SupplierQuotationRecipient Recipient(Guid supplierId) =>
        new(supplierId, "Atacado Alfa", null, null, QuotationLinkToken.Create().Hash);

    private static QuotationRequestEntity CreateRequest(DateOnly? deadline) =>
        QuotationRequestEntity.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Reposição",
            null,
            "Marina",
            "marina@empresa.com.br",
            deadline,
            null,
            null,
            [new QuotationRequestItemSnapshot(Guid.NewGuid(), "Arroz", 1, "un")],
            IssuedAt);
}
