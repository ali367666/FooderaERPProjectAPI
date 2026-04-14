using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return await _context.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}