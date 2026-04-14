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

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}