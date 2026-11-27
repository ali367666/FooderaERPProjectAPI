using Application.MyProfile;

namespace Application.Common.Interfaces;

/// <summary>The signed-in user's own profile. Callers pass only the user id taken from the token.</summary>
public interface IProfileService
{
    Task<ProfileDto?> GetAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Sets (or, with null, removes) the profile photo. False when the user does not exist.</summary>
    Task<bool> SetAvatarAsync(int userId, string? avatarUrl, CancellationToken cancellationToken);
}
