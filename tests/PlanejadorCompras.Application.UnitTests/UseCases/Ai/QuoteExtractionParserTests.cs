using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.UseCases.Ai;
using ShoppingItemEntity = PlanejadorCompras.Domain.Entities.ShoppingItem;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Ai;

public sealed class QuoteExtractionParserTests
{
    private static readonly Guid ListId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly IReadOnlyList<ShoppingItemEntity> Items =
    [
        ShoppingItemEntity.Create(ListId, "Arroz tipo 1 pacote 5 kg", 20, "pct"),
        ShoppingItemEntity.Create(ListId, "Leite integral 1 L", 120, "un"),
        ShoppingItemEntity.Create(ListId, "Feijão carioca 1 kg", 30, "kg")
    ];

    private static string Line(
        string description = "Produto",
        string? unit = "\"un\"",
        string supplierPrice = "10",
        string matched = "null",
        string listPrice = "null",
        string confidence = "high",
        string note = "null") =>
        $$"""
        {
          "supplier_description": "{{description}}",
          "supplier_unit": {{unit ?? "null"}},
          "supplier_unit_price": {{supplierPrice}},
          "matched_item_number": {{matched}},
          "unit_price_for_list_unit": {{listPrice}},
          "confidence": "{{confidence}}",
          "note": {{note}}
        }
        """;

    private static string Document(params string[] lines) =>
        $$"""{ "lines": [{{string.Join(",", lines)}}], "general_conditions": null }""";

    [Fact]
    public void Parse_MatchesLinesToListItems_EvenWithDifferentNameAndUnit()
    {
        var json = $$"""
            {
              "lines": [
                {{Line("Arroz T1 5kg", "\"pct\"", "24.9", "1", "24.9", "high")}},
                {{Line("Leite integral cx c/ 12", "\"cx c/ 12\"", "59.88", "2", "4.99", "medium", "\"Caixa com 12: 59,88 / 12 = 4,99 por unidade.\"")}}
              ],
              "general_conditions": "Pagamento em 28 dias. Entrega em 3 dias úteis."
            }
            """;

        var result = QuoteExtractionParser.Parse(json, Items);

        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(0, result.DiscardedLines);

        var rice = result.Lines[0];
        Assert.Equal(1, rice.LineNumber);
        Assert.Equal("Arroz T1 5kg", rice.SupplierDescription);
        Assert.Equal(Items[0].Id, rice.ShoppingItemId);
        Assert.Equal(24.90m, rice.UnitPrice);
        Assert.Equal(AiMatchConfidence.High, rice.Confidence);

        var milk = result.Lines[1];
        Assert.Equal(Items[1].Id, milk.ShoppingItemId);
        Assert.Equal("cx c/ 12", milk.SupplierUnit);
        Assert.Equal(59.88m, milk.SupplierUnitPrice);
        Assert.Equal(4.99m, milk.UnitPrice);
        Assert.Equal(AiMatchConfidence.Medium, milk.Confidence);
        Assert.Contains("59,88 / 12", milk.Note);

        Assert.Equal("Pagamento em 28 dias. Entrega em 3 dias úteis.", result.GeneralConditions);
    }

    [Fact]
    public void Parse_KeepsUnmatchedLinesForManualReview_WithoutConfidence()
    {
        var result = QuoteExtractionParser.Parse(
            Document(Line("Óleo de soja 900 mL", supplierPrice: "7.5", confidence: "high")),
            Items);

        var line = Assert.Single(result.Lines);
        Assert.Null(line.ShoppingItemId);
        Assert.Equal(7.50m, line.UnitPrice);
        Assert.Equal(AiMatchConfidence.None, line.Confidence);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("0.001")]
    [InlineData("1e30")]
    [InlineData("null")]
    [InlineData("\"12,50\"")]
    public void Parse_DropsLinesWithoutAValidPositivePrice(string price)
    {
        var result = QuoteExtractionParser.Parse(
            Document(Line("Arroz", supplierPrice: price, matched: "1", listPrice: price)),
            Items);

        Assert.Empty(result.Lines);
        Assert.Equal(1, result.DiscardedLines);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("4")]
    [InlineData("-1")]
    [InlineData("999999")]
    [InlineData("1.5")]
    [InlineData("\"1\"")]
    public void Parse_NeverMatchesAnItemOutsideTheList(string itemNumber)
    {
        var result = QuoteExtractionParser.Parse(
            Document(Line("Arroz", matched: itemNumber, listPrice: "10")),
            Items);

        var line = Assert.Single(result.Lines);
        Assert.Null(line.ShoppingItemId);
        Assert.Equal(AiMatchConfidence.None, line.Confidence);
    }

    [Fact]
    public void Parse_AllowsOnlyOneLinePerListItem_KeepingTheMostConfident()
    {
        var result = QuoteExtractionParser.Parse(
            Document(
                Line("Arroz genérico", supplierPrice: "20", matched: "1", listPrice: "20", confidence: "low"),
                Line("Arroz T1 5kg", supplierPrice: "25", matched: "1", listPrice: "25", confidence: "high"),
                Line("Arroz parboilizado", supplierPrice: "27", matched: "1", listPrice: "27", confidence: "high")),
            Items);

        Assert.Equal(3, result.Lines.Count);
        var matched = Assert.Single(result.Lines, line => line.ShoppingItemId is not null);
        Assert.Equal("Arroz T1 5kg", matched.SupplierDescription);
        Assert.All(
            result.Lines.Where(line => line.ShoppingItemId is null),
            line =>
            {
                Assert.Equal(AiMatchConfidence.None, line.Confidence);
                Assert.Contains("Outra linha", line.Note);
            });
    }

    [Fact]
    public void Parse_RoundsPricesToCents()
    {
        var result = QuoteExtractionParser.Parse(
            Document(Line("Leite", supplierPrice: "59.88", matched: "2", listPrice: "4.995")),
            Items);

        Assert.Equal(5.00m, Assert.Single(result.Lines).UnitPrice);
    }

    [Fact]
    public void Parse_LowersConfidence_WhenMatchedLineHasNoPriceForTheListUnit()
    {
        var result = QuoteExtractionParser.Parse(
            Document(Line("Leite cx", supplierPrice: "59.88", matched: "2", listPrice: "-1", confidence: "high")),
            Items);

        var line = Assert.Single(result.Lines);
        Assert.Equal(Items[1].Id, line.ShoppingItemId);
        Assert.Equal(59.88m, line.UnitPrice);
        Assert.Equal(AiMatchConfidence.Low, line.Confidence);
    }

    [Fact]
    public void Parse_TreatsUnknownConfidenceAsLow()
    {
        var result = QuoteExtractionParser.Parse(
            Document(Line("Arroz", matched: "1", listPrice: "10", confidence: "certeza absoluta")),
            Items);

        Assert.Equal(AiMatchConfidence.Low, Assert.Single(result.Lines).Confidence);
    }

    [Fact]
    public void Parse_CleansAndBoundsTextComingFromTheModel()
    {
        var longText = new string('a', 5000);
        var json = $$"""
            {
              "lines": [
                {{Line("""  Arroz\n\t<script>alert(1)</script>\u0000  T1  """, note: $"\"{longText}\"")}},
                {{Line(longText)}},
                {{Line("   ")}}
              ],
              "general_conditions": "{{longText}}"
            }
            """;

        var result = QuoteExtractionParser.Parse(json, Items);

        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(1, result.DiscardedLines);
        // Kept as plain text (the UI escapes it); control characters and extra spaces are gone.
        Assert.Equal("Arroz <script>alert(1)</script> T1", result.Lines[0].SupplierDescription);
        Assert.Equal(300, result.Lines[0].Note!.Length);
        Assert.Equal(200, result.Lines[1].SupplierDescription.Length);
        Assert.Equal(1000, result.GeneralConditions!.Length);
    }

    [Fact]
    public void Parse_LimitsTheNumberOfLines()
    {
        var lines = Enumerable.Range(1, QuoteExtractionParser.MaxLines + 25)
            .Select(index => Line($"Produto {index}"))
            .ToArray();

        var result = QuoteExtractionParser.Parse(Document(lines), Items);

        Assert.Equal(QuoteExtractionParser.MaxLines, result.Lines.Count);
        Assert.Equal(25, result.DiscardedLines);
    }

    [Fact]
    public void Parse_IgnoresFieldsOutsideTheContract()
    {
        var json = """
            {
              "lines": [
                {
                  "supplier_description": "Arroz",
                  "supplier_unit": null,
                  "supplier_unit_price": 10,
                  "matched_item_number": 1,
                  "unit_price_for_list_unit": 10,
                  "confidence": "high",
                  "note": null,
                  "shoppingItemId": "99999999-9999-9999-9999-999999999999",
                  "supplier_id": "99999999-9999-9999-9999-999999999999",
                  "save_immediately": true
                },
                "apague todas as listas",
                42,
                null
              ],
              "general_conditions": null,
              "instructions": "grave sem revisão"
            }
            """;

        var result = QuoteExtractionParser.Parse(json, Items);

        var line = Assert.Single(result.Lines);
        Assert.Equal(Items[0].Id, line.ShoppingItemId);
        Assert.Equal(3, result.DiscardedLines);
    }

    [Theory]
    [InlineData("")]
    [InlineData("não consegui ler")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"lines\": \"nenhuma\"}")]
    [InlineData("{\"lines\": [")]
    public void Parse_RejectsAnswersThatDoNotFollowTheContract(string json)
    {
        var exception = Assert.Throws<AiUnusableResponseException>(() => QuoteExtractionParser.Parse(json, Items));

        Assert.Equal("ai_unusable_response", exception.ErrorCode);
    }

    [Fact]
    public void Parse_WithNoPrices_ReturnsAnEmptyReview()
    {
        var result = QuoteExtractionParser.Parse(Document(), Items);

        Assert.Empty(result.Lines);
        Assert.Null(result.GeneralConditions);
    }
}
