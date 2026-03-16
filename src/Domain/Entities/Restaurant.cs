using Domain.Common;

namespace Domain.Entities;

public class Restaurant : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // Foreign key
    public int CompanyId { get; set; }

    // Navigation
    public Company Company { get; set; } = default!;
}
