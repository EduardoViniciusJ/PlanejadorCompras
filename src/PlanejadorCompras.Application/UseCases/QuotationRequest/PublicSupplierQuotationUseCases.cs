using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Services;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.ItemQuote;
using PlanejadorCompras.Domain.Repositories.QuotationRequest;
using PlanejadorCompras.Domain.Repositories.ShoppingItem;
using PlanejadorCompras.Domain.Repositories.ShoppingList;
using PlanejadorCompras.Domain.Repositories.ShoppingListSupplier;
using PlanejadorCompras.Domain.Repositories.Supplier;
using PlanejadorCompras.Domain.Rules;
using ItemQuoteEntity = PlanejadorCompras.Domain.Entities.ItemQuote;
using ShoppingListSupplierEntity = PlanejadorCompras.Domain.Entities.ShoppingListSupplier;

namespace PlanejadorCompras.Application.UseCases.QuotationRequest;

/// <summary>
/// Turns the token of a public link into the single supplier quotation it gives access to.
/// There is no authenticated user here: the token is the only credential.
/// </summary>
public sealed class PublicSupplierQuotationResolver(
    IQuotationRequestRepository repository,
    TimeProvider timeProvider)
{
    public async Task<(SupplierQuotationAccess Access, DateTime UtcNow)> ResolveAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (!QuotationLinkToken.HasValidFormat(token))
        {
            throw NotFound();
        }

        var access = await repository.GetSupplierQuotationByTokenHashAsync(
            QuotationLinkToken.Hash(token!),
            cancellationToken);
        if (access is null)
        {
            throw NotFound();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (access.Quotation.IsRevoked)
        {
            throw new GoneException(
                "This quotation link was revoked.",
                "quotation_link_revoked");
        }

        if (access.Quotation.IsExpiredAt(utcNow))
        {
            throw new GoneException(
                "This quotation link has expired.",
                "quotation_link_expired");
        }

        return (access, utcNow);
    }

    internal static PublicSupplierQuotationResponseDto Map(
        SupplierQuotationAccess access,
        DateTime utcNow)
    {
        var prices = access.Quotation.ResponseItems.ToDictionary(
            item => item.QuotationRequestItemId,
            item => item.UnitPrice);

        return new PublicSupplierQuotationResponseDto(
            access.Request.Code,
            access.Request.BuyerName,
            access.Quotation.SupplierName,
            access.Request.ShoppingListName,
            access.Request.ResponseDeadline,
            access.Request.DeliveryAddress,
            access.Request.Instructions,
            access.Request.AcceptsResponsesAt(utcNow),
            access.Quotation.RespondedAtUtc,
            QuotationRequestResponseMapper.ToTerms(access.Quotation.Terms),
            access.Request.Items
                .OrderBy(item => item.Position)
                .Select(item => new PublicQuotationItemDto(
                    item.Id,
                    item.Name,
                    item.Quantity,
                    item.Unit,
                    prices.TryGetValue(item.Id, out var price) ? price : null))
                .ToList());
    }

    private static NotFoundException NotFound() =>
        new("Quotation link not found.", "quotation_link_not_found");
}

public sealed class GetPublicSupplierQuotationUseCase(
    PublicSupplierQuotationResolver resolver,
    IUnitOfWork unitOfWork)
{
    public async Task<PublicSupplierQuotationResponseDto> ExecuteAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        var (access, utcNow) = await resolver.ResolveAsync(token, cancellationToken);

        if (!access.Quotation.ViewedAtUtc.HasValue)
        {
            access.Quotation.MarkViewed(utcNow);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return PublicSupplierQuotationResolver.Map(access, utcNow);
    }
}

public sealed class SubmitPublicSupplierQuotationUseCase(
    PublicSupplierQuotationResolver resolver,
    IShoppingListRepository shoppingListRepository,
    IShoppingItemRepository shoppingItemRepository,
    ISupplierRepository supplierRepository,
    IShoppingListSupplierRepository shoppingListSupplierRepository,
    IItemQuoteRepository itemQuoteRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<PublicSupplierQuotationResponseDto> ExecuteAsync(
        string? token,
        SubmitSupplierQuotationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (access, utcNow) = await resolver.ResolveAsync(token, cancellationToken);

        if (!access.Request.AcceptsResponsesAt(utcNow))
        {
            throw new ConflictException(
                "The response deadline has passed.",
                "quotation_response_deadline_passed");
        }

        var prices = ValidatePrices(access, request);
        var terms = BuildTerms(request, utcNow);
        var previouslyAnsweredItemIds = access.Quotation.ResponseItems
            .Select(item => item.QuotationRequestItemId)
            .ToHashSet();

        access.Quotation.Respond(prices, terms, utcNow);
        await ApplyToPriceMapAsync(
            access,
            prices,
            terms,
            previouslyAnsweredItemIds,
            utcNow,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return PublicSupplierQuotationResolver.Map(access, utcNow);
    }

    private static List<SupplierQuotationPrice> ValidatePrices(
        SupplierQuotationAccess access,
        SubmitSupplierQuotationRequestDto request)
    {
        if (request.Prices is null
            || request.Prices.Count == 0
            || request.Prices.Count > SupplierQuotationRules.MaximumPricesPerResponse)
        {
            throw new BadRequestException(
                "Inform at least one price.",
                "quotation_response_without_prices");
        }

        var requestItemIds = access.Request.Items.Select(item => item.Id).ToHashSet();
        var seen = new HashSet<Guid>();
        var prices = new List<SupplierQuotationPrice>(request.Prices.Count);
        foreach (var price in request.Prices)
        {
            if (price is null || !requestItemIds.Contains(price.ItemId) || !seen.Add(price.ItemId))
            {
                throw new BadRequestException(
                    "The response has an item that is not part of this quotation.",
                    "quotation_response_invalid_item");
            }

            if (price.UnitPrice < SupplierQuotationRules.MinimumUnitPrice
                || price.UnitPrice > SupplierQuotationRules.MaximumUnitPrice
                || decimal.Round(price.UnitPrice, SupplierQuotationRules.UnitPriceScale) != price.UnitPrice)
            {
                throw new BadRequestException(
                    "The response has an invalid price.",
                    "quotation_response_invalid_price");
            }

            prices.Add(new SupplierQuotationPrice(price.ItemId, price.UnitPrice));
        }

        return prices;
    }

    private static CommercialTerms BuildTerms(
        SubmitSupplierQuotationRequestDto request,
        DateTime utcNow)
    {
        var today = DateOnly.FromDateTime(utcNow + SupplierQuotationRules.DeadlineUtcOffset);
        if (request.ProposalValidUntil is { } validUntil
            && (validUntil < today || validUntil > today.AddYears(5)))
        {
            throw new BadRequestException(
                "The proposal validity date is invalid.",
                "quotation_response_invalid_terms");
        }

        try
        {
            return CommercialTerms.Create(
                request.DeliveryDays,
                request.PaymentTerms,
                request.FreightCost,
                request.MinimumOrderValue,
                request.ProposalValidUntil,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            throw new BadRequestException(
                "The commercial terms are invalid.",
                "quotation_response_invalid_terms",
                exception);
        }
    }

    /// <summary>
    /// Copies the answer to the price map of the source list. Everything is rechecked against
    /// the owner of the request, because the list, its items or the supplier may have been
    /// changed or removed after the request was issued. The answer itself is always kept.
    /// </summary>
    private async Task ApplyToPriceMapAsync(
        SupplierQuotationAccess access,
        IReadOnlyCollection<SupplierQuotationPrice> prices,
        CommercialTerms terms,
        IReadOnlySet<Guid> previouslyAnsweredItemIds,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (access.Request.SourceShoppingListId is not { } shoppingListId)
        {
            return;
        }

        var ownerId = access.Request.UserId;
        var supplierId = access.Quotation.SupplierId;
        var shoppingList = await shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
        var supplier = await supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        if (shoppingList is null
            || shoppingList.UserId != ownerId
            || supplier is null
            || supplier.UserId != ownerId)
        {
            return;
        }

        var link = await shoppingListSupplierRepository.GetLinkAsync(
            shoppingListId,
            supplierId,
            cancellationToken);
        if (link is null)
        {
            link = ShoppingListSupplierEntity.Create(shoppingListId, supplierId);
            await shoppingListSupplierRepository.AddAsync(link, cancellationToken);
        }

        link.UpdateTerms(terms, utcNow);

        var liveItemIds = (await shoppingItemRepository.GetByShoppingListIdAsync(
                shoppingListId,
                cancellationToken))
            .Select(item => item.Id)
            .ToHashSet();
        var existingQuotes = (await itemQuoteRepository.GetBySupplierInShoppingListAsync(
                shoppingListId,
                supplierId,
                cancellationToken))
            .GroupBy(quote => quote.ShoppingItemId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var priceByRequestItem = prices.ToDictionary(
            price => price.QuotationRequestItemId,
            price => price.UnitPrice);

        foreach (var requestItem in access.Request.Items)
        {
            if (requestItem.SourceShoppingItemId is not { } shoppingItemId
                || !liveItemIds.Contains(shoppingItemId))
            {
                continue;
            }

            var quotes = existingQuotes.GetValueOrDefault(shoppingItemId) ?? [];
            if (priceByRequestItem.TryGetValue(requestItem.Id, out var unitPrice))
            {
                if (quotes.Count == 0)
                {
                    await itemQuoteRepository.AddAsync(
                        ItemQuoteEntity.Create(shoppingItemId, supplierId, unitPrice),
                        cancellationToken);
                    continue;
                }

                quotes[0].Update(shoppingItemId, supplierId, unitPrice);
                foreach (var duplicate in quotes.Skip(1))
                {
                    itemQuoteRepository.Remove(duplicate);
                }
            }
            else if (previouslyAnsweredItemIds.Contains(requestItem.Id))
            {
                // The supplier withdrew a price it had informed before through this link.
                foreach (var quote in quotes)
                {
                    itemQuoteRepository.Remove(quote);
                }
            }
        }
    }
}
