namespace Application.Common.Interfaces.Abstracts.Repositories;

public interface IPrinterStationTypeRepository
{
    Task AddAsync(Domain.Entities.PrinterStationType stationType, CancellationToken cancellationToken);
    Task<Domain.Entities.PrinterStationType?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken);
    Task<List<Domain.Entities.PrinterStationType>> GetAllByCompanyAsync(int companyId, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(int companyId, string name, int? excludeId, CancellationToken cancellationToken);
    void Update(Domain.Entities.PrinterStationType stationType);
    void Delete(Domain.Entities.PrinterStationType stationType);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
