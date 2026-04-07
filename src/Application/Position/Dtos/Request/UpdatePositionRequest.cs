namespace Application.Position.Dtos;

public class UpdatePositionRequest
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}