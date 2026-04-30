namespace Application.Position.Dtos;

public class PositionResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? DepartmentName { get; set; }
    public string? CompanyName { get; set; }
}