namespace Application.Departments.Dtos;

public class DepartmentResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}