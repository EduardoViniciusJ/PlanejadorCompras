using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.RefreshToken;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly PlanejadorComprasDbContext _context;

    public RefreshTokenRepository(PlanejadorComprasDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<bool> IsSessionActiveAsync(
        Guid sessionId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .AsNoTracking()
            .AnyAsync(
                token => token.SessionId == sessionId
                    && token.RevokedAt == null
                    && token.ExpiresAt > nowUtc,
                cancellationToken);
    }

    public async Task RevokeSessionAsync(
        Guid sessionId,
        RefreshTokenRevocationReason reason,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(token => token.SessionId == sessionId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(reason, nowUtc);
        }
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        RefreshTokenRevocationReason reason,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(reason, nowUtc);
        }
    }

    public async Task RemoveExpiredForUserAsync(
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens
            .Where(token => token.UserId == userId && token.ExpiresAt <= nowUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
