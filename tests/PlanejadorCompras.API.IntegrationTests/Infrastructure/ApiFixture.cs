using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PlanejadorCompras.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PlanejadorCompras.API.IntegrationTests.Infrastructure;

/// <summary>
/// Starts a disposable PostgreSQL container, applies the real EF migrations to it and hosts the
/// API in memory against that database. Nothing here touches the shared development database:
/// the connection string always points at the container's random port.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17.11-alpine3.24")
        .WithDatabase("planejador_compras_tests")
        .WithUsername("planejador_tests")
        .WithPassword(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)))
        .Build();

    /// <summary>API with Google login replaced by a fake identity provider.</summary>
    public ApiFactory Api { get; private set; } = null!;

    /// <summary>API exactly as it starts with no Google credentials configured.</summary>
    public ApiFactory ApiWithoutGoogle { get; private set; } = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Program.cs reads these while building the host, before WebApplicationFactory can add
        // configuration sources, so they are provided as environment variables of the test process.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", ApiFactory.EnvironmentName);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable(
            "Authentication__Jwt__SecretKey",
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
        Environment.SetEnvironmentVariable("Authentication__Google__ClientId", string.Empty);
        Environment.SetEnvironmentVariable("Authentication__Google__ClientSecret", string.Empty);
        Environment.SetEnvironmentVariable("Authentication__Google__RedirectUri", string.Empty);
        Environment.SetEnvironmentVariable("Resend__ApiKey", string.Empty);
        Environment.SetEnvironmentVariable("Frontend__BaseUrl", "https://app.example.test");
        // No leeway: a reused refresh token is always treated as reuse, which keeps the tests
        // deterministic. The leeway itself is covered by unit tests with a controlled clock.
        Environment.SetEnvironmentVariable("Authentication__RefreshToken__ReuseLeewaySeconds", "0");

        await using (var context = CreateDbContext(ConnectionString))
        {
            await context.Database.MigrateAsync();
        }

        Api = new ApiFactory(useFakeGoogle: true);
        ApiWithoutGoogle = new ApiFactory(useFakeGoogle: false);

        EnsureHostUsesTheContainer(Api);
        EnsureHostUsesTheContainer(ApiWithoutGoogle);
    }

    public async Task DisposeAsync()
    {
        if (Api is not null)
        {
            await Api.DisposeAsync();
        }

        if (ApiWithoutGoogle is not null)
        {
            await ApiWithoutGoogle.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    /// <summary>Creates an empty database in the container, for tests that drive migrations themselves.</summary>
    public async Task<string> CreateEmptyDatabaseAsync(string name)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
        await command.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ConnectionString;
    }

    public static PlanejadorComprasDbContext CreateDbContext(string connectionString) =>
        new(new DbContextOptionsBuilder<PlanejadorComprasDbContext>().UseNpgsql(connectionString).Options);

    private void EnsureHostUsesTheContainer(ApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var hostConnectionString = scope.ServiceProvider
            .GetRequiredService<PlanejadorComprasDbContext>()
            .Database
            .GetConnectionString();

        var expected = new NpgsqlConnectionStringBuilder(ConnectionString);
        var actual = new NpgsqlConnectionStringBuilder(hostConnectionString);

        if (actual.Host != expected.Host || actual.Port != expected.Port || actual.Database != expected.Database)
        {
            throw new InvalidOperationException(
                "The API under test is not connected to the test container. Aborting to protect other databases.");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
