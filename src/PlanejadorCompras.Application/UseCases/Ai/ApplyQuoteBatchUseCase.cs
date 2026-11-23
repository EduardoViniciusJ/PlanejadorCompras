using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories;
using PlanejadorCompras.Domain.Repositories.ItemQuote;
using PlanejadorCompras.Domain.Repositories.ShoppingItem;
using PlanejadorCompras.Domain.Repositories.ShoppingListSupplier;
using ItemQuoteEntity = PlanejadorCompras.Domain.Entities.ItemQuote;

namespace PlanejadorCompras.Application.UseCases.Ai;

/// <summary>
/// Saves, in one commit, the prices the buyer confirmed for one supplier of a list.
/// This is the only way prices proposed by the AI get stored, and it validates
/// everything again: ownership, supplier link, item membership, price range, duplicates.
/// </summary>
public sealed class ApplyQuoteBatchUseCase(
    IShoppingListAccessService shoppingListAccessService,
    ISupplierAccessService supplierAccessService,
    IShoppingListSupplierRepository shoppingListSupplierRepository,
    IShoppingItemRepository shoppingItemRepository,
    IItemQuoteRepository itemQuoteRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<QuoteBatchResponseDto> ExecuteAsync(
        Guid shoppingListId,
        QuoteBatchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lines = request.Quotes;
        if (lines is null || lines.Count is < 1 or > QuoteBatchRequestDto.MaxLines)
        {
            throw new BadRequestException(
                $"Provide 1 to {QuoteBatchRequestDto.MaxLines} quotes.",
                "invalid_quote_batch");
        }

        if (lines.Select(line => line.ShoppingItemId).Distinct().Count() != lines.Count)
        {
            throw new BadRequestException(
                "Each shopping item can appear only once.",
                "duplicate_quote_item");
        }

        await shoppingListAccessService.GetForCurrentUserAsync(shoppingListId, cancellationToken);
        await supplierAccessService.GetForCurrentUserAsync(request.SupplierId, cancellationToken);
        if (!await shoppingListSupplierRepository.ExistsAsync(
                shoppingListId,
                request.SupplierId,
                cancellationToken))
        {
            throw new NotFoundException(
                "Supplier is not assigned to this shopping list.",
                "shopping_list_supplier_not_found");
        }

        var listItems = await shoppingItemRepository.GetByShoppingListIdAsync(shoppingListId, cancellationToken);
        var listItemIds = listItems.Select(item => item.Id).ToHashSet();
        if (lines.Any(line => !listItemIds.Contains(line.ShoppingItemId)))
        {
            throw new NotFoundException("Shopping item not found.", "shopping_item_not_found");
        }

        var existingQuotes = (await itemQuoteRepository.GetByShoppingListIdAsync(shoppingListId, cancellationToken))
            .Where(quote => quote.SupplierId == request.SupplierId)
            .GroupBy(quote => quote.ShoppingItemId)
            .ToDictionary(group => group.Key, group => group.OrderBy(quote => quote.CreatedAt).First());

        // Build every change first: an invalid price throws here and nothing is committed.
        var created = new List<ItemQuoteEntity>();
        var updated = new List<ItemQuoteEntity>();
        foreach (var line in lines)
        {
            if (existingQuotes.TryGetValue(line.ShoppingItemId, out var existing))
            {
                existing.Update(line.ShoppingItemId, request.SupplierId, line.UnitPrice);
                updated.Add(existing);
            }
            else
            {
                created.Add(ItemQuoteEntity.Create(line.ShoppingItemId, request.SupplierId, line.UnitPrice));
            }
        }

        foreach (var quote in updated)
        {
            await itemQuoteRepository.UpdateAsync(quote, cancellationToken);
        }

        foreach (var quote in created)
        {
            await itemQuoteRepository.AddAsync(quote, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);

        return new QuoteBatchResponseDto(created.Count, updated.Count);
    }
}
