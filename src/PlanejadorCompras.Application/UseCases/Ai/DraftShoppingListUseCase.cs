using PlanejadorCompras.Application.Features.Ai.Contracts;

namespace PlanejadorCompras.Application.UseCases.Ai;

/// <summary>
/// Proposes a shopping list from a free-text description or a photo. Nothing is created
/// here: the user reviews the proposal and the list is created through the regular endpoints.
/// </summary>
public sealed class DraftShoppingListUseCase(
    AiInputPreparer inputPreparer,
    AiUsageService aiUsageService)
{
    public async Task<ShoppingListDraftResponseDto> ExecuteAsync(
        string? text,
        AiUploadedFile? file,
        CancellationToken cancellationToken = default)
    {
        aiUsageService.EnsureEnabled();

        var input = inputPreparer.Prepare(text, file);
        var json = await aiUsageService.CompleteJsonAsync(
            AiPrompts.BuildShoppingListDraft(input),
            cancellationToken);

        var result = ShoppingListDraftParser.Parse(json);
        var usage = await aiUsageService.GetStatusAsync(cancellationToken);

        return new ShoppingListDraftResponseDto(
            result.SuggestedName,
            result.Items,
            result.DiscardedItems,
            usage);
    }
}
