using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Domain.Entities.User user);
    string GenerateRefreshToken();
}