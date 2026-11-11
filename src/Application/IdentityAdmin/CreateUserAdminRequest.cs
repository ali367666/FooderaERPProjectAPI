namespace Application.IdentityAdmin;

public class CreateUserAdminRequest
{
    /// <summary>Optional — company staff are shown by their username (or linked employee name).</summary>
    public string? FullName { get; set; }
    public string UserName { get; set; } = default!;
    public string? Email { get; set; }
    /// <summary>
    /// Only for accounts that sign in with a real password (the SuperAdmins). Company staff use
    /// their Code: the same code on the POS and in the admin panel (no date added).
    /// </summary>
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
    public int? WarehouseId { get; set; }
    public List<int>? RoleIds { get; set; }
}
