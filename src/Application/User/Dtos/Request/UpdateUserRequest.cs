using Domain.Enums;

namespace Application.User.Dtos.Request;

public class UpdateUserRequest
{
    public string FullName { get; set; } = default!;

    public EmployeeWorkplaceType WorkplaceType { get; set; }

    public int CompanyId { get; set; }

    public int? RestaurantId { get; set; }
}
