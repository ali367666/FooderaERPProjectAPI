namespace Application.Common.Interfaces.Abstracts.Services;

public interface IReservationTableGuard
{
    /// <summary>
    /// Throws when the table is reserved inside the company's block window ("Rezerv qadağası") — no
    /// order may be opened on it, or moved onto it, until the guest is seated or the reservation ends.
    /// </summary>
    Task EnsureTableFreeAsync(int companyId, int tableId, CancellationToken cancellationToken);
}
