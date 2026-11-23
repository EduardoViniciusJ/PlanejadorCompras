using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PlanejadorCompras.API.IntegrationTests.Infrastructure;
using PlanejadorCompras.Domain.Security;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.API.IntegrationTests;

/// <summary>Audit finding M5: the database must never hold a usable token.</summary>
[Collection(ApiCollection.Name)]
public sealed class TokenStorageTests(ApiFixture fixture)
{
    private readonly ApiFactory _api = fixture.Api;

    [Fact]
    public async Task Database_ShouldStoreOnlyHashes_OfConfirmationResetAndRefreshTokens()
    {
        var browser = _api.CreateBrowser();
        var email = AuthTestExtensions.NewEmail();

        (await browser.RegisterAsync(email)).EnsureSuccessStatusCode();
        var confirmationToken = _api.Emails.LastTokenSentTo(email);

        using (var scope = _api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PlanejadorComprasDbContext>();
            var stored = await db.UserTokens.SingleAsync(token => token.User.Email == email);
            Assert.Equal("EmailConfirmation", stored.Type);
            Assert.NotEqual(confirmationToken, stored.TokenHash);
            Assert.Equal(SecretToken.Hash(confirmationToken), stored.TokenHash);
        }

        // The value stored in the database is not accepted as a token.
        var withHash = await browser.PostAsJsonAsync(
            "/api/auth/confirm-email",
            new { token = SecretToken.Hash(confirmationToken) });
        Assert.Equal(HttpStatusCode.BadRequest, withHash.StatusCode);

        (await browser.PostAsJsonAsync("/api/auth/confirm-email", new { token = confirmationToken }))
            .EnsureSuccessStatusCode();
        (await browser.PostAsJsonAsync("/api/auth/forgot-password", new { email })).EnsureSuccessStatusCode();
        var resetToken = _api.Emails.LastTokenSentTo(email);
        var session = (await browser.LoginAsync(email)).ReadSessionCookies();

        using (var scope = _api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PlanejadorComprasDbContext>();

            var storedReset = await db.UserTokens.SingleAsync(token => token.User.Email == email);
            Assert.Equal("PasswordReset", storedReset.Type);
            Assert.Equal(SecretToken.Hash(resetToken), storedReset.TokenHash);

            var storedRefresh = await db.RefreshTokens.SingleAsync(token => token.User.Email == email);
            Assert.Equal(SecretToken.Hash(session.RefreshToken), storedRefresh.TokenHash);

            // Looking at every text column of both tables, no raw token appears anywhere.
            var everything = await ReadAllTextAsync(db, "UserTokens") + await ReadAllTextAsync(db, "RefreshTokens");
            Assert.DoesNotContain(confirmationToken, everything, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(resetToken, everything, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(session.RefreshToken, everything, StringComparison.OrdinalIgnoreCase);
        }

        // A stolen refresh-token hash is not a refresh token either.
        var withRefreshHash = await _api.CreateRawClient().SendWithCookieAsync(
            HttpMethod.Post,
            "/api/auth/refresh",
            $"refresh_token={SecretToken.Hash(session.RefreshToken)}");
        Assert.Equal(HttpStatusCode.Unauthorized, withRefreshHash.StatusCode);
    }

    /// <summary>
    /// The migration must convert rows that already exist: a link e-mailed before the deploy keeps
    /// working afterwards, and the plain-text value is gone.
    /// </summary>
    [Fact]
    public async Task Migration_ShouldHashExistingPlainTextTokensInPlace()
    {
        var connectionString = await fixture.CreateEmptyDatabaseAsync($"migration_{Guid.NewGuid():N}");
        var userId = Guid.NewGuid();
        var legacyTokens = new[] { SecretToken.Generate(), SecretToken.Generate() };

        await using var db = ApiFixture.CreateDbContext(connectionString);
        var migrator = db.GetService<IMigrator>();

        // Database as it was before this change, holding two plain-text tokens.
        await migrator.MigrateAsync("AddManualAuthentication");
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "Users" ("Id", "Email", "PasswordHash", "IsEmailConfirmed", "CreatedAt")
            VALUES ({0}, 'legacy@example.test', 'hash', false, now());
            INSERT INTO "UserTokens" ("Id", "UserId", "Token", "Type", "ExpiresAt")
            VALUES ({1}, {0}, {3}, 'EmailConfirmation', now() + interval '1 day'),
                   ({2}, {0}, {4}, 'PasswordReset', now() + interval '1 hour');
            """,
            userId, Guid.NewGuid(), Guid.NewGuid(), legacyTokens[0], legacyTokens[1]);

        await migrator.MigrateAsync();

        var stored = await db.UserTokens.AsNoTracking().OrderBy(token => token.Type).ToListAsync();
        Assert.Equal(2, stored.Count);
        Assert.Equal(SecretToken.Hash(legacyTokens[0]), stored[0].TokenHash);
        Assert.Equal(SecretToken.Hash(legacyTokens[1]), stored[1].TokenHash);
        var everything = await ReadAllTextAsync(db, "UserTokens");
        Assert.DoesNotContain(legacyTokens[0], everything, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(legacyTokens[1], everything, StringComparison.OrdinalIgnoreCase);

        // Rolling back works and does not leave hashes where the old code expects raw tokens.
        await migrator.MigrateAsync("AddManualAuthentication");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var count = new NpgsqlCommand("""SELECT count(*) FROM "UserTokens" """, connection);
        Assert.Equal(0L, await count.ExecuteScalarAsync());
        await using var column = new NpgsqlCommand(
            """
            SELECT count(*) FROM information_schema.columns
            WHERE table_name = 'UserTokens' AND column_name = 'Token'
            """,
            connection);
        Assert.Equal(1L, await column.ExecuteScalarAsync());
        await using var refreshTable = new NpgsqlCommand(
            "SELECT count(*) FROM information_schema.tables WHERE table_name = 'RefreshTokens'",
            connection);
        Assert.Equal(0L, await refreshTable.ExecuteScalarAsync());
    }

    private static async Task<string> ReadAllTextAsync(PlanejadorComprasDbContext db, string table)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = new NpgsqlCommand($"SELECT t::text FROM \"{table}\" t", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetString(0));
        }

        return string.Join('\n', rows);
    }
}
