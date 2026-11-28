using Domain.Common;

namespace Domain.Entities;

/// <summary>A holiday of an employee, from the first to the last day (both included).</summary>
public class EmployeeLeave : CompanyEntity<int>
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = default!;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? Note { get; set; }
}
