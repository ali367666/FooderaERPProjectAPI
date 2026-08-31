using Domain.Common;

namespace Domain.Entities;

public class CounterpartyCategory : CompanyEntity<int>
{
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
