namespace Application.IdentityAdmin;

public class UserListItemDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public List<string> Roles { get; set; } = new();
    public int? LinkedEmployeeId { get; set; }
    public string? Code { get; set; }
    public string? RfidCardId { get; set; }
    public bool CanAccessAdminPanel { get; set; }
    public bool CanAccessFrontOffice { get; set; }
    public Domain.Enums.EmployeeWorkplaceType WorkplaceType { get; set; }
    public int? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
}

public class UserDetailDto : UserListItemDto
{
}

public class CreateUserAdminRequest
{
    public string FullName { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public int CompanyId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Code { get; set; }
    public string? RfidCardId { get; set; }
    public bool CanAccessAdminPanel { get; set; } = true;
    public bool CanAccessFrontOffice { get; set; } = false;
    public Domain.Enums.EmployeeWorkplaceType WorkplaceType { get; set; } = Domain.Enums.EmployeeWorkplaceType.HeadOffice;
    public int? RestaurantId { get; set; }
}

public class UpdateUserAdminRequest
{
    public string FullName { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Password { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public int CompanyId { get; set; }
    public int? EmployeeId { get; set; }
    public string? Code { get; set; }
    public string? RfidCardId { get; set; }
    public bool CanAccessAdminPanel { get; set; } = true;
    public bool CanAccessFrontOffice { get; set; } = false;
    public Domain.Enums.EmployeeWorkplaceType WorkplaceType { get; set; } = Domain.Enums.EmployeeWorkplaceType.HeadOffice;
    public int? RestaurantId { get; set; }
}

public class RoleListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? NormalizedName { get; set; }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = default!;
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = default!;
}

public class UserRoleRowDto
{
    public int UserId { get; set; }
    public string UserFullName { get; set; } = default!;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = default!;
}

public class AssignUserRoleRequest
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
}

public class RemoveUserRoleRequest
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
}

public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Module { get; set; } = default!;
    public string Action { get; set; } = default!;
}

public class UpdateRolePermissionsRequest
{
    public List<int> PermissionIds { get; set; } = new();
}
