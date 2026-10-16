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
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}
