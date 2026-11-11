namespace Application.Employees.Dtos;

public class EmployeeResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? FatherName { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsActive { get; set; }

    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = default!;

    public int PositionId { get; set; }
    public string PositionName { get; set; } = default!;

    /// <summary>The role the position stands for (the job title); null for an old position that is not a role yet.</summary>
    public int? RoleId { get; set; }

    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = default!;

    public int? UserId { get; set; }
}