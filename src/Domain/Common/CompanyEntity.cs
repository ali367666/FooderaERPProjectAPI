using Domain.Entities;

namespace Domain.Common;

public class CompanyEntity<T> : BaseEntity<T>
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = default!;
}
