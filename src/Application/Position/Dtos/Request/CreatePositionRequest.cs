namespace Application.Position.Dtos;

public class CreatePositionRequest
{
    public int CompanyId { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}