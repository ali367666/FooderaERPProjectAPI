namespace Application.Common.Interfaces;

/// <summary>
/// Ready-made lookup lists every company starts with — counterparty categories and printer
/// station types. Only filled in while the company has none of its own, so it never re-adds
/// entries a company deleted or renamed.
/// </summary>
public interface ICompanyDefaultsSeeder
{
    Task EnsureDefaultsAsync(int companyId, CancellationToken cancellationToken = default);
}
