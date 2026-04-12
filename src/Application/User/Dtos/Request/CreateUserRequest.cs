using Domain.Enums;

namespace Application.User.Dtos.Request;

public class CreateUserRequest
{
    public int EmployeeId { get; set; }
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public EmployeeWorkplaceType WorkplaceType { get; set; }
    public int? RestaurantId { get; set; }
}
