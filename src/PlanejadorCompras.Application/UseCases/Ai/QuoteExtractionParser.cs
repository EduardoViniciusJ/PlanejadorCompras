using System.Text.Json;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Domain.Rules;
using ShoppingItemEntity = PlanejadorCompras.Domain.Entities.ShoppingItem;

namespace PlanejadorCompras.Application.UseCases.Ai;

public sealed record QuoteExtractionResult(
    IReadOnlyList<ExtractedQuoteLineDto> Lines,
    string? GeneralConditions,
    int DiscardedLines);

/// <summary>
/// Turns the model's JSON into review lines, trusting nothing in it: prices must be
/// positive and within the domain range, a match must point to an item of this list and
/// each item can be matched by a single line. Whatever fails is dropped or downgraded to
/// "no match"; it never becomes a saved price.
/// </summary>
public static class QuoteExtractionParser
{
    public const int MaxLines = 300;
    private const int DescriptionMaxLength = 200;
    private const int UnitMaxLength = 40;
    private const int NoteMaxLength = 300;
    private const int ConditionsMaxLength = 1000;
    private const string DuplicateNote = "Outra linha já foi casada com este item. Escolha o item manualmente, se for o caso.";

    public static QuoteExtractionResult Parse(string json, IReadOnlyList<ShoppingItemEntity> items)
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
                || !root.TryGetProperty("lines", out var linesElement)
                || linesElement.ValueKind != JsonValueKind.Array)
            {
                throw AiUnusableResponseException.Unreadable();
            }

            var candidates = new List<Candidate>();
            var discarded = 0;

            foreach (var element in linesElement.EnumerateArray())
            {
                if (candidates.Count >= MaxLines)
                {
                    discarded++;
                    continue;
                }

                var candidate = ReadLine(element, items);
                if (candidate is null)
                {
                    discarded++;
                    continue;
                }

                candidates.Add(candidate);
            }

            RemoveDuplicateMatches(candidates);

            var lines = candidates
                .Select((candidate, index) => new ExtractedQuoteLineDto(
                    index + 1,
                    candidate.Description,
                    candidate.SupplierUnit,
                    candidate.SupplierUnitPrice,
                    candidate.ShoppingItemId,
                    candidate.UnitPrice,
                    candidate.ShoppingItemId is null ? AiMatchConfidence.None : candidate.Confidence,
                    candidate.Note))
                .ToArray();

            var conditions = AiText.Clean(ReadString(root, "general_conditions"), ConditionsMaxLength);
            return new QuoteExtractionResult(lines, conditions, discarded);
        }
    }

    private static Candidate? ReadLine(JsonElement element, IReadOnlyList<ShoppingItemEntity> items)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var description = AiText.Clean(ReadString(element, "supplier_description"), DescriptionMaxLength);
        if (description is null)
        {
            return null;
        }

        var supplierPrice = ReadPrice(element, "supplier_unit_price");
        var listUnitPrice = ReadPrice(element, "unit_price_for_list_unit");

        Guid? shoppingItemId = null;
        if (element.TryGetProperty("matched_item_number", out var numberElement)
            && numberElement.ValueKind == JsonValueKind.Number
            && numberElement.TryGetInt32(out var itemNumber)
            && itemNumber >= 1
            && itemNumber <= items.Count)
        {
            shoppingItemId = items[itemNumber - 1].Id;
        }

        // Without a match the converted price has no meaning; fall back to the quoted one.
        var unitPrice = shoppingItemId is null
            ? supplierPrice ?? listUnitPrice
            : listUnitPrice ?? supplierPrice;
        if (unitPrice is null)
        {
            return null;
        }

        var confidence = ReadString(element, "confidence") switch
        {
            AiMatchConfidence.High => AiMatchConfidence.High,
            AiMatchConfidence.Medium => AiMatchConfidence.Medium,
            _ => AiMatchConfidence.Low
        };

        // A match without a usable price for the list unit was not really converted.
        if (shoppingItemId is not null && listUnitPrice is null)
        {
            confidence = AiMatchConfidence.Low;
        }

        return new Candidate
        {
            Description = description,
            SupplierUnit = AiText.Clean(ReadString(element, "supplier_unit"), UnitMaxLength),
            SupplierUnitPrice = supplierPrice,
            ShoppingItemId = shoppingItemId,
            UnitPrice = unitPrice.Value,
            Confidence = confidence,
            Note = AiText.Clean(ReadString(element, "note"), NoteMaxLength)
        };
    }

    private static void RemoveDuplicateMatches(List<Candidate> candidates)
    {
        var matchedGroups = candidates
            .Where(candidate => candidate.ShoppingItemId is not null)
            .GroupBy(candidate => candidate.ShoppingItemId!.Value);

        foreach (var group in matchedGroups)
        {
            // The most confident line keeps the match; on a tie, the first one read.
            var winner = group.OrderByDescending(candidate => ConfidenceRank(candidate.Confidence)).First();
            foreach (var candidate in group)
            {
                if (ReferenceEquals(candidate, winner))
                {
                    continue;
                }

                candidate.ShoppingItemId = null;
                candidate.UnitPrice = candidate.SupplierUnitPrice ?? candidate.UnitPrice;
                candidate.Note = DuplicateNote;
            }
        }
    }

    private static int ConfidenceRank(string confidence) => confidence switch
    {
        AiMatchConfidence.High => 3,
        AiMatchConfidence.Medium => 2,
        _ => 1
    };

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    /// <summary>Reads a price and returns null unless it is a valid quote price.</summary>
    private static decimal? ReadPrice(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetDecimal(out var value))
        {
            return null;
        }

        if (value <= 0 || value > ItemQuoteRules.MaximumUnitPrice)
        {
            return null;
        }

        var rounded = Math.Round(value, ItemQuoteRules.UnitPriceScale, MidpointRounding.AwayFromZero);
        return rounded < ItemQuoteRules.MinimumUnitPrice || rounded > ItemQuoteRules.MaximumUnitPrice
            ? null
            : rounded;
    }

    private sealed class Candidate
    {
        public required string Description { get; init; }
        public string? SupplierUnit { get; init; }
        public decimal? SupplierUnitPrice { get; init; }
        public Guid? ShoppingItemId { get; set; }
        public decimal UnitPrice { get; set; }
        public required string Confidence { get; init; }
        public string? Note { get; set; }
    }
}
