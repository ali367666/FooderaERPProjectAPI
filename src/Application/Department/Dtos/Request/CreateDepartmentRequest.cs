namespace Application.Departments.Dtos;

public class CreateDepartmentRequest
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}