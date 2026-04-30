namespace Application.Common.Interfaces;

/// <summary>
/// Resolves the current ASP.NET Identity user id from JWT claims, with fallback to UserManager (email / username).
/// </summary>
public interface IAuthenticatedUserAccessor
{
    Task<int> ResolveUserIdAsync(CancellationToken cancellationToken = default);
}
