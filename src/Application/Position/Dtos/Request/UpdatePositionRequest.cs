namespace Application.Position.Dtos;

public class UpdatePositionRequest
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>
    /// Target company for the position. Only honored for a SuperAdmin caller — a tenant Admin is
    /// always confined to their own company regardless of what this carries.
    /// </summary>
    public int? CompanyId { get; set; }
}