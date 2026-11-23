using System.Net;
using System.Net.Http.Json;
using PlanejadorCompras.API.IntegrationTests.Infrastructure;

namespace PlanejadorCompras.API.IntegrationTests;

/// <summary>A duplicate that only the database catches must answer 409, never 500.</summary>
[Collection(ApiCollection.Name)]
public sealed class DuplicateConflictTests(ApiFixture fixture)
{
    private readonly ApiFactory _api = fixture.Api;

    [Fact]
    public async Task ConcurrentCreationOfTheSameSupplier_ShouldCreateOne_AndAnswerConflictForTheRest()
    {
        var browser = _api.CreateBrowser();
        await _api.SignUpAndLoginAsync(browser, AuthTestExtensions.NewEmail());

        // The use case checks "is this name taken?" and then inserts. Requests that run the check
        // at the same time all pass it; the unique index stops all but one at the insert.
        var responses = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
            browser.PostAsJsonAsync("/api/suppliers", new { name = "Fornecedor Duplicado" })));
        var statuses = responses.Select(response => response.StatusCode).ToList();

        Assert.DoesNotContain(HttpStatusCode.InternalServerError, statuses);
        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.Created));
        Assert.Equal(11, statuses.Count(status => status == HttpStatusCode.Conflict));

        foreach (var conflict in responses.Where(response => response.StatusCode == HttpStatusCode.Conflict))
        {
            Assert.NotNull(await conflict.ReadErrorCodeAsync());
        }
    }

    [Fact]
    public async Task RegisteringAnExistingEmailWithExtraSpaces_ShouldAnswerLikeAnyOtherRegistration()
    {
        var email = AuthTestExtensions.NewEmail();
        var browser = _api.CreateBrowser();
        await _api.RegisterConfirmedAsync(browser, email);
        var emailsBefore = _api.Emails.SentTo(email).Count;

        // The lookup used the raw value while the insert used the trimmed one, so this used to
        // hit the unique index (500) and reveal that the account exists.
        var padded = await _api.CreateBrowser().RegisterAsync($"  {email.ToUpperInvariant()} ");

        Assert.Equal(HttpStatusCode.OK, padded.StatusCode);
        Assert.Equal(emailsBefore, _api.Emails.SentTo(email).Count);

        // The original account is untouched and login also tolerates the padding.
        Assert.Equal(HttpStatusCode.OK, (await _api.CreateBrowser().LoginAsync($" {email} ")).StatusCode);
    }
}
