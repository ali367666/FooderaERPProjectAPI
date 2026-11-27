namespace Application.MyProfile;

public class ProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Code { get; set; }
    public string? AvatarUrl { get; set; }
    public List<string> Roles { get; set; } = new();

    public Domain.Enums.EmployeeWorkplaceType WorkplaceType { get; set; }
    public string? RestaurantName { get; set; }
    public string? WarehouseName { get; set; }
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public DateTime? HireDate { get; set; }

    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyCode { get; set; }
    public string? CompanyTaxNumber { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhoneNumber { get; set; }
    public string? CompanyEmail { get; set; }
}
