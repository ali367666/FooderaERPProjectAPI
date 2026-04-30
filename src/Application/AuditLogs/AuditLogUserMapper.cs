using Application.AuditLogs.Dtos.Response;
using Domain.Entities;

namespace Application.AuditLogs;

internal static class AuditLogUserMapper
{
    /// <summary>
    /// Maps identity user: trimmed full name and email when present.
    /// </summary>
    public static (string? UserFullName, string? UserEmail) Map(Domain.Entities.User? user)
    {
        if (user == null)
            return (null, null);

        var email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim();
        var fullName = string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName.Trim();

        return (fullName, email);
    }

    public static AuditLogResponse ToResponse(AuditLog x)
    {
        var (userFullName, userEmail) = Map(x.User);

        return new AuditLogResponse
        {
            Id = x.Id,
            EntityName = x.EntityName,
            EntityId = x.EntityId,
            ActionType = x.ActionType,
            OldValues = x.OldValues,
            NewValues = x.NewValues,
            Message = x.Message,
            UserId = x.UserId,
            UserFullName = userFullName,
            UserEmail = userEmail,
            CompanyId = x.CompanyId,
            CorrelationId = x.CorrelationId,
            IsSuccess = x.IsSuccess,
            CreatedAtUtc = x.CreatedAtUtc,
        };
    }
}
