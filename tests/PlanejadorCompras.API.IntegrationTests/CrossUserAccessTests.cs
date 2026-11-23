using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PlanejadorCompras.API.IntegrationTests.Infrastructure;

namespace PlanejadorCompras.API.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class CrossUserAccessTests(ApiFixture fixture)
{
    private readonly ApiFactory _api = fixture.Api;

    [Fact]
    public async Task ResourceOfAnotherUser_ShouldLookLikeItDoesNotExist()
    {
        var alice = _api.CreateBrowser();
        var bob = _api.CreateBrowser();
        await _api.SignUpAndLoginAsync(alice, AuthTestExtensions.NewEmail("alice"));
        await _api.SignUpAndLoginAsync(bob, AuthTestExtensions.NewEmail("bob"));

        var created = await alice.PostAsJsonAsync("/api/shopping-lists", new { name = "Lista da Alice" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var listId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var supplier = await alice.PostAsJsonAsync("/api/suppliers", new { name = "Fornecedor da Alice" });
        Assert.Equal(HttpStatusCode.Created, supplier.StatusCode);
        var supplierId = (await supplier.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Bob knows the ids but owns nothing: every route answers 404, never 403 or data.
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/shopping-lists/{listId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/shopping-lists/{listId}/detail")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/shopping-lists/{listId}/equalization")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await bob.PutAsJsonAsync($"/api/shopping-lists/{listId}", new { name = "Agora é do Bob" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/shopping-lists/{listId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/suppliers/{supplierId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/suppliers/{supplierId}")).StatusCode);

        // Bob's own listings do not include Alice's data.
        var bobLists = await bob.GetStringAsync("/api/shopping-lists");
        var bobSuppliers = await bob.GetStringAsync("/api/suppliers");
        Assert.DoesNotContain(listId.ToString(), bobLists);
        Assert.DoesNotContain(supplierId.ToString(), bobSuppliers);

        // Alice still has everything, untouched.
        var aliceList = await alice.GetFromJsonAsync<JsonElement>($"/api/shopping-lists/{listId}");
        Assert.Equal("Lista da Alice", aliceList.GetProperty("name").GetString());
        Assert.Equal(HttpStatusCode.OK, (await alice.GetAsync($"/api/suppliers/{supplierId}")).StatusCode);
    }

    [Fact]
    public async Task ProtectedRoutes_ShouldRequireAuthentication()
    {
        var anonymous = _api.CreateBrowser();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/shopping-lists")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/suppliers")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync("/api/shopping-lists", new { name = "Sem login" })).StatusCode);
    }
}
