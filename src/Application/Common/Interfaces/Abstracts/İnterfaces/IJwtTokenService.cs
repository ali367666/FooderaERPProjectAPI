using Application.Auth.Dtos.Responce;
using Domain.Entities;

namespace Application.Common.Interfaces.Abstracts.İnterfaces;

public interface IJwtTokenService
{
    Task<LoginResponse> CreateTokenAsync(Domain.Entities.User user, IEnumerable<string> permissions, string? ipAddress = null);
    string GenerateRefreshToken();
}