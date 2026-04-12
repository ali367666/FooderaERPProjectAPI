namespace Application.Departments.Dtos;

public class CreateDepartmentRequest
{
    
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}