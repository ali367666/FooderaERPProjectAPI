using Application.Common.Exceptions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Enums;

namespace Application.Banquets.Services;

public sealed class BanquetTableChecker : IBanquetTableChecker
{
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IReservationRepository _reservationRepository;

    public BanquetTableChecker(
        IRestaurantTableRepository tableRepository,
        IOrderRepository orderRepository,
        IReservationRepository reservationRepository)
    {
        _tableRepository = tableRepository;
        _orderRepository = orderRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<List<Domain.Entities.RestaurantTable>> LoadTablesAsync(
        int companyId, int restaurantId, IReadOnlyCollection<int> tableIds, CancellationToken cancellationToken)
    {
        var ids = tableIds.Distinct().ToList();
        if (ids.Count == 0)
            throw new BadRequestException("Banket üçün ən azı bir masa seçin.");

        var tables = new List<Domain.Entities.RestaurantTable>();
        foreach (var id in ids)
        {
            var table = await _tableRepository.GetByIdAsync(id, companyId, cancellationToken);
            if (table is null || table.RestaurantId != restaurantId || !table.IsActive
                || table.Type is RestaurantTableType.Delivery or RestaurantTableType.TakeAway)
                throw new BadRequestException("Seçilmiş masalardan biri bu filial üçün tapılmadı.");
            tables.Add(table);
        }

        return tables;
    }

    public async Task<string?> GetBusyReasonNowAsync(
        int companyId, Domain.Entities.RestaurantTable table, CancellationToken cancellationToken)
    {
        if (await _orderRepository.HasOpenOrderForTableAsync(table.Id, companyId, cancellationToken))
            return "Dolu: bu masada açıq sifariş var.";

        return null;
    }

    public async Task<string?> GetBookedReasonAsync(
        int companyId, Domain.Entities.RestaurantTable table, DateTime start, int durationMinutes, CancellationToken cancellationToken)
    {
        var clash = await _reservationRepository.FindOverlappingAsync(
            companyId, table.Id, start, durationMinutes, null, cancellationToken);

        return clash is null
            ? null
            : $"Rezerv: {clash.ReservationTime:hh\\:mm} üçün {clash.GuestName} adına.";
    }
}
