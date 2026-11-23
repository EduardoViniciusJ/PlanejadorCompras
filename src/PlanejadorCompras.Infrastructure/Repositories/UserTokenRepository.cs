using Microsoft.EntityFrameworkCore;
using PlanejadorCompras.Domain.Entities;
using PlanejadorCompras.Domain.Repositories.User;
using PlanejadorCompras.Infrastructure.Persistence;

namespace PlanejadorCompras.Infrastructure.Repositories;

public sealed class UserTokenRepository : IUserTokenRepository
{
    private readonly PlanejadorComprasDbContext _context;

    public UserTokenRepository(PlanejadorComprasDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserToken userToken, CancellationToken cancellationToken = default)
    {
        await _context.UserTokens.AddAsync(userToken, cancellationToken);
    }

    public async Task<UserToken?> GetByTokenHashAsync(string tokenHash, string type, CancellationToken cancellationToken = default)
    {
        return await _context.UserTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.Type == type, cancellationToken);
    }

    public async Task RemoveByUserAndTypeAsync(
        Guid userId,
        string type,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.UserTokens
            .Where(token => token.UserId == userId && token.Type == type)
            .ToListAsync(cancellationToken);

        _context.UserTokens.RemoveRange(tokens);
    }

    public async Task RemoveAllByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.UserTokens
            .Where(token => token.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.UserTokens.RemoveRange(tokens);
    }

    public void Remove(UserToken userToken)
    {
        _context.UserTokens.Remove(userToken);
    }
}
