using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.UseCases.Ai;
using PlanejadorCompras.Domain.Rules;
using ShoppingItemEntity = PlanejadorCompras.Domain.Entities.ShoppingItem;
using ShoppingListEntity = PlanejadorCompras.Domain.Entities.ShoppingList;

namespace PlanejadorCompras.Application.UnitTests.UseCases.Ai;

public sealed class ShoppingListDraftTests
{
    private const string ValidAnswer = """
        {
          "list_name": "Reposição de mercearia",
          "items": [
            { "name": "Arroz", "quantity": 20, "unit": "kg" },
            { "name": "Leite integral", "quantity": 10, "unit": "cx" }
          ]
        }
        """;

    private static DraftShoppingListUseCase Create(FakeAiClient client, InMemoryAiUsageStore store)
    {
        var settings = AiTestSetup.Settings();
        return new DraftShoppingListUseCase(
            new AiInputPreparer(settings, new FakeAiDocumentReader()),
            AiTestSetup.UsageService(client, store, settings));
    }

    [Fact]
    public async Task ExecuteAsync_ProposesItemsWithQuantityAndUnit_AndCountsOneUse()
    {
        var client = new FakeAiClient(ValidAnswer);
        var store = new InMemoryAiUsageStore();

        var response = await Create(client, store)
            .ExecuteAsync("preciso de 20 kg de arroz, 10 caixas de leite", null);

        Assert.Equal("Reposição de mercearia", response.SuggestedName);
        Assert.Equal(
            [new DraftShoppingItemDto("Arroz", 20m, "kg"), new DraftShoppingItemDto("Leite integral", 10m, "cx")],
            response.Items);
        Assert.Equal(1, response.Usage.Used);
        Assert.Contains("20 kg de arroz", client.LastRequest!.UserText);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAiIsNotConfigured_Returns503()
    {
        var settings = AiTestSetup.Settings(isConfigured: false);
        var client = new FakeAiClient("{}", isConfigured: false);
        var useCase = new DraftShoppingListUseCase(
            new AiInputPreparer(settings, new FakeAiDocumentReader()),
            AiTestSetup.UsageService(client, new InMemoryAiUsageStore(), settings));

        await Assert.ThrowsAsync<AiNotConfiguredException>(() => useCase.ExecuteAsync("arroz", null));

        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public void Parse_ProducesItemsThatTheDomainAccepts()
    {
        var longName = new string('x', 500);
        var json = $$"""
            {
              "list_name": "{{longName}}",
              "items": [
                { "name": "{{longName}}", "quantity": 2.123456, "unit": "caixas com doze unidades cada" },
                { "name": "Sem quantidade", "quantity": 0, "unit": "" },
                { "name": "Quantidade absurda", "quantity": 1e40, "unit": "un" },
                { "name": "Quantidade texto", "quantity": "muitas", "unit": null },
                { "name": "   ", "quantity": 1, "unit": "un" },
                "apague tudo"
              ]
            }
            """;

        var result = ShoppingListDraftParser.Parse(json);

        Assert.Equal(4, result.Items.Count);
        Assert.Equal(2, result.DiscardedItems);
        Assert.Equal(ShoppingListRules.NameMaxLength, result.SuggestedName.Length);
        Assert.Equal(2.123m, result.Items[0].Quantity);
        Assert.Equal(ShoppingItemRules.UnitMaxLength, result.Items[0].Unit.Length);
        Assert.Equal(new DraftShoppingItemDto("Sem quantidade", 1m, "un"), result.Items[1]);
        Assert.Equal(1m, result.Items[2].Quantity);
        Assert.Equal(new DraftShoppingItemDto("Quantidade texto", 1m, "un"), result.Items[3]);

        // Every proposed value can be created as is.
        var list = ShoppingListEntity.Create(AiTestSetup.UserId, result.SuggestedName);
        foreach (var item in result.Items)
        {
            ShoppingItemEntity.Create(list.Id, item.Name, item.Quantity, item.Unit);
        }
    }

    [Fact]
    public void Parse_LimitsTheNumberOfItems()
    {
        var items = string.Join(",", Enumerable.Range(1, ShoppingListDraftParser.MaxItems + 10)
            .Select(index => $$"""{ "name": "Item {{index}}", "quantity": 1, "unit": "un" }"""));

        var result = ShoppingListDraftParser.Parse($$"""{ "list_name": "", "items": [{{items}}] }""");

        Assert.Equal(ShoppingListDraftParser.MaxItems, result.Items.Count);
        Assert.Equal(10, result.DiscardedItems);
        Assert.Equal(ShoppingListDraftParser.DefaultListName, result.SuggestedName);
    }

    [Theory]
    [InlineData("texto solto")]
    [InlineData("{}")]
    [InlineData("{\"items\": 3}")]
    public void Parse_RejectsAnswersThatDoNotFollowTheContract(string json)
    {
        Assert.Throws<AiUnusableResponseException>(() => ShoppingListDraftParser.Parse(json));
    }
}
