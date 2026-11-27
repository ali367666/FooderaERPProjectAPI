using Application.IdentityAdmin;

namespace Application.Common.Interfaces;

public interface IIdentityAdminService
{
    Task<List<UserListItemDto>> GetUsersAsync(int? companyIdFilter, CancellationToken cancellationToken = default);

    Task<UserDetailDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Ok, int? UserId, string? Error, Dictionary<string, string[]>? FieldErrors)> CreateUserAsync(
        CreateUserAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error, Dictionary<string, string[]>? FieldErrors)> UpdateUserAsync(
        int id,
        UpdateUserAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteUserAsync(int id, CancellationToken cancellationToken = default);

    Task<List<RoleListItemDto>> GetRolesAsync(int? companyId = null, CancellationToken cancellationToken = default);

    Task<RoleListItemDto?> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Ok, int? RoleId, string? Error)> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateRoleAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);

    Task<List<UserRoleRowDto>> GetUserRoleMappingsAsync(CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> AssignUserRoleAsync(AssignUserRoleRequest request, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> RemoveUserRoleAsync(RemoveUserRoleRequest request, CancellationToken cancellationToken = default);

    Task<List<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    Task<List<int>> GetRolePermissionIdsAsync(int roleId, CancellationToken cancellationToken = default);

    Task<(bool Ok, string? Error)> UpdateRolePermissionsAsync(
        int roleId,
        List<int> permissionIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clones the global template roles (Admin/Manager/Waiter/Kitchen/Cashier/User) into new,
    /// company-owned roles for the given company, copying each template's permissions. Called once
    /// right after a new company is created so it gets a full, independent role set immediately.
    /// </summary>
    Task CloneDefaultRolesForCompanyAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The position that stands for a company role (the employee's job title), created when it does not exist yet.
    /// Error when the role is not found or is not a company role.
    /// </summary>
    Task<(int? PositionId, string? Error)> EnsurePositionForRoleAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>Gives every role of the company its position and every old position its role (idempotent).</summary>
    Task BackfillRolePositionsAsync(int companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks that a new company SuperAdmin account can be created (email/username free, password
    /// meets the policy) — run before the company itself is saved, so a bad owner doesn't leave an
    /// orphan company behind. Returns null when OK, otherwise the error message.
    /// </summary>
    Task<string?> ValidateCompanySuperAdminAsync(string userName, string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a manager's credentials for an approval at the till: the user must belong to the company, be
    /// active, know the password and hold the permission (the company SuperAdmin counts as holding all).
    /// Returns the approver's id, or an error text.
    /// </summary>
    Task<(int? UserId, string? Error)> VerifyApproverAsync(
        int companyId, string userName, string password, string requiredPermission, CancellationToken cancellationToken = default);

    /// <summary>Whether the user holds the permission (the company SuperAdmin counts as holding all).</summary>
    Task<bool> UserHoldsPermissionAsync(int userId, int companyId, string permission, CancellationToken cancellationToken = default);

    /// <summary>Ids of the company's active users who hold the permission (the company SuperAdmin counts as holding all).</summary>
    Task<List<int>> GetUserIdsWithPermissionAsync(int companyId, string permission, CancellationToken cancellationToken = default);

    /// <summary>Creates the company's own SuperAdmin user and assigns the global CompanySuperAdmin role.</summary>
    Task<(bool Ok, string? Error)> CreateCompanySuperAdminAsync(
        int companyId, string fullName, string userName, string email, string password,
        CancellationToken cancellationToken = default);
}
