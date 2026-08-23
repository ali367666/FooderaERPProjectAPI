namespace Application.Common.Interfaces.Abstracts.İnterfaces;

/// <summary>
/// Signs/verifies short-lived tokens used to authorize anonymous "click a link in email" actions
/// (e.g. approve/reject from an email) without requiring the recipient to log in.
/// </summary>
public interface IMailActionTokenService
{
    string GenerateToken(int entityId, TimeSpan validity);

    bool IsValid(int entityId, string? token);
}
