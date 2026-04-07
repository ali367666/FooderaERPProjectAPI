using Domain.Common;

namespace Domain.Entities;

public class Employee : CompanyEntity<int>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? FatherName { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }

    public bool IsActive { get; set; } = true;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public int PositionId { get; set; }
    public Position Position { get; set; } = default!;

    public int? UserId { get; set; }
    public User? User { get; set; }
}