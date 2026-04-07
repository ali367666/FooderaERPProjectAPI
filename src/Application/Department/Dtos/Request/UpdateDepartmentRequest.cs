namespace Application.Departments.Dtos;

public class UpdateDepartmentRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}