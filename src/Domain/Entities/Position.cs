using Domain.Common;

namespace Domain.Entities;

public class Position : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}