using Domain.Enums;

namespace Application.User.Dtos.Responce;

public sealed class UpdateUserResponseDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = default!;

    public EmployeeWorkplaceType WorkplaceType { get; set; }

    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = default!;

    public int? RestaurantId { get; set; }

    public string? RestaurantName { get; set; }
}