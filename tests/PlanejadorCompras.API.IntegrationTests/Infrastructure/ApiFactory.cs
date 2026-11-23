using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.API.IntegrationTests.Infrastructure;

public sealed class ApiFactory(bool useFakeGoogle) : WebApplicationFactory<Program>
{
    public const string EnvironmentName = "Testing";
    public const string ClientIpHeader = "X-Test-Client-Ip";

    private static int _clientCounter;

    public CapturingEmailService Emails { get; } = new();

    public FakeGoogleIdentityProvider Google { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);

            if (useFakeGoogle)
            {
                services.RemoveAll<IGoogleAuthorizationCodeExchanger>();
                services.RemoveAll<IGoogleTokenValidator>();
                services.AddSingleton<IGoogleAuthorizationCodeExchanger>(Google);
                services.AddSingleton<IGoogleTokenValidator>(Google);
            }

            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>();
        });
    }

    /// <summary>
    /// A browser: keeps cookies between requests, sends the CSRF header the frontend always sends,
    /// and comes from its own IP address so the per-IP rate limits of one test do not affect another.
    /// </summary>
    public HttpClient CreateBrowser() => CreateApiClient(handleCookies: true);

    /// <summary>
    /// A client with no cookie jar, for replaying captured cookies by hand (an attacker with a
    /// stolen token, or a browser holding only one of the two cookies).
    /// </summary>
    public HttpClient CreateRawClient() => CreateApiClient(handleCookies: false);

    private HttpClient CreateApiClient(bool handleCookies)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = handleCookies
        });

        var id = Interlocked.Increment(ref _clientCounter);
        client.DefaultRequestHeaders.Add(ClientIpHeader, $"10.{(id >> 16) & 0xFF}.{(id >> 8) & 0xFF}.{id & 0xFF}");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XmlHttpRequest");

        return client;
    }

    /// <summary>TestServer has no remote address; this gives each test client the one it asks for.</summary>
    private sealed class TestClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(ClientIpHeader, out var value)
                    && IPAddress.TryParse(value.ToString(), out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                await nextMiddleware();
            });

            next(app);
        };
    }
}
