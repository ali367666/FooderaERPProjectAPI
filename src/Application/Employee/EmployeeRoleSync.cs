using Application.Common.Interfaces;
using Application.IdentityAdmin;
using Microsoft.Extensions.Logging;

namespace Application.Employees;

/// <summary>An employee's job title is a role: the account linked to the employee holds that role, and swaps it when the title changes.</summary>
public static class EmployeeRoleSync
{
    public static async Task ApplyAsync(
        IIdentityAdminService identity, ILogger logger, int? userId, int? oldRoleId, int? newRoleId, CancellationToken ct)
    {
        if (userId is not > 0 || oldRoleId == newRoleId) return;
        try
        {
            if (newRoleId is > 0)
                await identity.AssignUserRoleAsync(new AssignUserRoleRequest { UserId = userId.Value, RoleId = newRoleId.Value }, ct);
            if (oldRoleId is > 0)
                await identity.RemoveUserRoleAsync(new RemoveUserRoleRequest { UserId = userId.Value, RoleId = oldRoleId.Value }, ct);
        }
        catch (Exception ex)
        {
            // The employee is saved either way; a role that could not be moved is shown on the user's own page.
            logger.LogWarning(ex, "Employee role could not be applied to user {UserId}", userId);
        }
    }
}
