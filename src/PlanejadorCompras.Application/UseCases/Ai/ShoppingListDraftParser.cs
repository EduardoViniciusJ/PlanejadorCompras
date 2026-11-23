using System.Text.Json;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Domain.Rules;

namespace PlanejadorCompras.Application.UseCases.Ai;

public sealed record ShoppingListDraftResult(
    string SuggestedName,
    IReadOnlyList<DraftShoppingItemDto> Items,
    int DiscardedItems);

/// <summary>
/// Turns the model's JSON into a list proposal that already respects the domain limits,
/// so every item can be created as is after the user reviews it.
/// </summary>
public static class ShoppingListDraftParser
{
    public const int MaxItems = 200;
    public const string DefaultListName = "Lista de compras";
    private const string DefaultUnit = "un";

    public static ShoppingListDraftResult Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw AiUnusableResponseException.Unreadable(exception);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("items", out var itemsElement)
                || itemsElement.ValueKind != JsonValueKind.Array)
            {
                throw AiUnusableResponseException.Unreadable();
            }

            var items = new List<DraftShoppingItemDto>();
            var discarded = 0;

            foreach (var element in itemsElement.EnumerateArray())
            {
                var item = items.Count < MaxItems ? ReadItem(element) : null;
                if (item is null)
                {
                    discarded++;
                    continue;
                }

                items.Add(item);
            }

            var name = AiText.Clean(ReadString(root, "list_name"), ShoppingListRules.NameMaxLength)
                ?? DefaultListName;

            return new ShoppingListDraftResult(name, items, discarded);
        }
    }

    private static DraftShoppingItemDto? ReadItem(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var name = AiText.Clean(ReadString(element, "name"), ShoppingItemRules.NameMaxLength);
        if (name is null)
        {
            return null;
        }

        var quantity = 1m;
        if (element.TryGetProperty("quantity", out var quantityElement)
            && quantityElement.ValueKind == JsonValueKind.Number
            && quantityElement.TryGetDecimal(out var parsedQuantity))
        {
            var rounded = Math.Round(parsedQuantity, ShoppingItemRules.QuantityScale, MidpointRounding.AwayFromZero);
            if (rounded >= ShoppingItemRules.MinimumQuantity && rounded <= ShoppingItemRules.MaximumQuantity)
            {
                quantity = rounded;
            }
        }

        var unit = AiText.Clean(ReadString(element, "unit"), ShoppingItemRules.UnitMaxLength) ?? DefaultUnit;

        return new DraftShoppingItemDto(name, quantity, unit);
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
