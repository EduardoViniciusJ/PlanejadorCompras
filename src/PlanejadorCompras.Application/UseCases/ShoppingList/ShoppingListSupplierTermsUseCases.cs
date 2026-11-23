using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.QuotationRequests.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Application.UseCases.QuotationRequest;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.ShoppingListSupplier;

namespace PlanejadorCompras.Application.UseCases.ShoppingList;

/// <summary>Commercial terms of every supplier linked to a list, for the price map.</summary>
public sealed class GetShoppingListSupplierTermsUseCase(
    IShoppingListSupplierRepository shoppingListSupplierRepository,
    IShoppingListAccessService shoppingListAccessService)
{
    public async Task<IReadOnlyCollection<ShoppingListSupplierTermsResponseDto>> ExecuteAsync(
        Guid shoppingListId,
        CancellationToken cancellationToken = default)
    {
        await shoppingListAccessService.GetForCurrentUserAsync(shoppingListId, cancellationToken);

        return (await shoppingListSupplierRepository.GetLinksAsync(shoppingListId, cancellationToken))
            .Select(ShoppingListSupplierTermsMapper.Map)
            .ToList();
    }
}

/// <summary>Lets the buyer type the terms a supplier informed outside the public link.</summary>
public sealed class UpdateShoppingListSupplierTermsUseCase(
    IShoppingListSupplierRepository shoppingListSupplierRepository,
    IShoppingListAccessService shoppingListAccessService,
    ISupplierAccessService supplierAccessService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ShoppingListSupplierTermsResponseDto> ExecuteAsync(
        Guid shoppingListId,
        Guid supplierId,
        CommercialTermsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await shoppingListAccessService.GetForCurrentUserAsync(shoppingListId, cancellationToken);
        await supplierAccessService.GetForCurrentUserAsync(supplierId, cancellationToken);

        var link = await shoppingListSupplierRepository.GetLinkAsync(
            shoppingListId,
            supplierId,
            cancellationToken);
        if (link is null)
        {
            throw new NotFoundException(
                "Supplier is not assigned to this shopping list.",
                "shopping_list_supplier_not_found");
        }

        link.UpdateTerms(
            CommercialTerms.Create(
                request.DeliveryDays,
                request.PaymentTerms,
                request.FreightCost,
                request.MinimumOrderValue,
                request.ProposalValidUntil,
                request.Notes),
            timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.CommitAsync(cancellationToken);

        return ShoppingListSupplierTermsMapper.Map(link);
    }
}

internal static class ShoppingListSupplierTermsMapper
{
    internal static ShoppingListSupplierTermsResponseDto Map(ShoppingListSupplier link) =>
        new(
            link.SupplierId,
            QuotationRequestResponseMapper.ToTerms(link.Terms),
            link.TermsUpdatedAtUtc);
}
