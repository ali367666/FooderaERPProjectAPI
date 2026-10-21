namespace Application.Employees.Dtos;

public class CreateEmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// The whole name in one field. Setting it fills <see cref="FirstName"/> and <see cref="LastName"/>
    /// (the first word / the rest), so a client may send either form.
    /// </summary>
    public string? FullName
    {
        get => $"{FirstName} {LastName}".Trim();
        set => (FirstName, LastName) = EmployeeNames.Split(value);
    }
    public string? FatherName { get; set; }

    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public DateTime HireDate { get; set; }

    public int DepartmentId { get; set; }
    public int PositionId { get; set; }
    public int RestaurantId { get; set; }

    public int? UserId { get; set; }
}