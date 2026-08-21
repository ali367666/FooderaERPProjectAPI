using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;


public class User : IdentityUser<int>
{
    public string FullName { get; set; } = default!;

    public EmployeeWorkplaceType WorkplaceType { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Code { get; set; }

    public string? RfidCardId { get; set; }

    public bool CanAccessAdminPanel { get; set; } = true;

    public bool CanAccessFrontOffice { get; set; } = false;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

}