using Domain.Common;

namespace Domain.Entities;

public class Position : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public int DepartmentId { get; set; }

    /// <summary>
    /// The company role this position stands for. A role IS the employee's job title: every company role has one position of the same
    /// name (kept in step with it), and the employee form picks the role. Null only for an old position nobody has turned into a role yet.
    /// </summary>
    public int? RoleId { get; set; }
    public Department Department { get; set; } = default!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}