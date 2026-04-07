namespace Application.Employees.Dtos;

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? FatherName { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public DateTime HireDate { get; set; }

    public int DepartmentId { get; set; }
    public int PositionId { get; set; }

    public int? UserId { get; set; }
}