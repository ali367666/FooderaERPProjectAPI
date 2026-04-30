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

    Task<List<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default);

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
}
