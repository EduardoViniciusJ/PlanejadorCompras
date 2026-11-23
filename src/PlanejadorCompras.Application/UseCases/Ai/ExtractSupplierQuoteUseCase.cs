using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;
using PlanejadorCompras.Domain.Repositories.ShoppingItem;

namespace PlanejadorCompras.Application.UseCases.Ai;

/// <summary>
/// Reads a supplier's answer (pasted text, PDF, photo or spreadsheet) and proposes prices
/// matched to the items of the list. It only reads: saving happens later, through
/// <see cref="ApplyQuoteBatchUseCase"/>, after the buyer reviews each line.
/// </summary>
public sealed class ExtractSupplierQuoteUseCase(
    IShoppingListAccessService shoppingListAccessService,
    IShoppingItemRepository shoppingItemRepository,
    AiInputPreparer inputPreparer,
    AiUsageService aiUsageService)
{
    public const int MaxListItems = 500;

    public async Task<QuoteExtractionResponseDto> ExecuteAsync(
        Guid shoppingListId,
        string? text,
        AiUploadedFile? file,
        CancellationToken cancellationToken = default)
    {
        aiUsageService.EnsureEnabled();
        await shoppingListAccessService.GetForCurrentUserAsync(shoppingListId, cancellationToken);

        // A stable order keeps the item numbers shown to the model deterministic.
        var items = (await shoppingItemRepository.GetByShoppingListIdAsync(shoppingListId, cancellationToken))
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToList();
        if (items.Count == 0)
        {
            throw new BadRequestException(
                "Adicione itens à lista antes de ler uma cotação.",
                "ai_list_without_items");
        }

        if (items.Count > MaxListItems)
        {
            throw new BadRequestException(
                $"A leitura por IA aceita listas com até {MaxListItems} itens.",
                "ai_list_too_long");
        }

        var input = inputPreparer.Prepare(text, file);
        var json = await aiUsageService.CompleteJsonAsync(
            AiPrompts.BuildQuoteExtraction(items, input),
            cancellationToken);

        var result = QuoteExtractionParser.Parse(json, items);
        var usage = await aiUsageService.GetStatusAsync(cancellationToken);

        return new QuoteExtractionResponseDto(
            result.Lines,
            result.GeneralConditions,
            result.DiscardedLines,
            usage);
    }
}
