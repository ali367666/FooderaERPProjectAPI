using Application.Auth.Dtos.Responce;

namespace Application.Common.Interfaces.Abstracts.Services;

public interface IAuthTokenIssuer
{
    Task<LoginResponse> IssueForUserAsync(Domain.Entities.User user, string? ipAddress, CancellationToken cancellationToken);
}
