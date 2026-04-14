using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(int userId, CancellationToken cancellationToken);
    void Update(RefreshToken refreshToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}