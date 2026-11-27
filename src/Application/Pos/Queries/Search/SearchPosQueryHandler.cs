using System.Text.RegularExpressions;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Pos.Dtos;
using Domain.Constants;
using Domain.Enums;
using MediatR;

namespace Application.Pos.Queries.Search;

public class SearchPosQueryHandler : IRequestHandler<SearchPosQuery, PosSearchResponse>
{
    private const int MaxOrders = 50;

    private readonly IRestaurantTableRepository _tableRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public SearchPosQueryHandler(
        IRestaurantTableRepository tableRepository,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _tableRepository = tableRepository;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PosSearchResponse> Handle(SearchPosQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        if (companyId == 0)
            throw new BadRequestException("CompanyId tapılmadı.");

        var words = SearchText.Fold(request.Term)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
            return new PosSearchResponse();

        var restaurantId = request.RestaurantId is > 0 ? request.RestaurantId : null;
        var canSeeSections = _currentUserService.HasPermission(AppPermissions.PosChangeDepartment);

        // Only what the tables screen would show this user: active tables, and without
        // Pos.ChangeDepartment only the tables outside every section.
        var allTables = (await _tableRepository.GetForSearchAsync(companyId, restaurantId, cancellationToken))
            .Where(t => t.IsActive && (canSeeSections || t.SectionId == null))
            .ToDictionary(t => t.Id);

        var activeOrders = await _orderRepository.GetActiveForTablesAsync(
            companyId, allTables.Keys.ToList(), cancellationToken);
        var latestOrderByTable = activeOrders
            .GroupBy(o => o.TableId)
            .ToDictionary(g => g.Key, g => g.First());

        bool Matches(string haystack) => words.All(w => haystack.Contains(w, StringComparison.Ordinal));
        static string Join(params string?[] parts) =>
            SearchText.Fold(string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p))));
        static string? FullName(Domain.Entities.Employee? e) =>
            e is null ? null : $"{e.FirstName} {e.LastName}".Trim();
        string OrderHaystack(Domain.Entities.Order o, Domain.Entities.RestaurantTable? t) => Join(
            o.OrderNumber, o.ReceiptNumber, FullName(o.Waiter), o.Counterparty?.Name, o.Note,
            o.DeliveryAddress, o.DeliveryPhone, t?.Name);

        // Virtual delivery / take-away tables never show on screen: their orders come back under
        // Orders only. Real tables match on their own details or on their open order's.
        var tables = allTables.Values
            .Where(t => t.Type != RestaurantTableType.Delivery && t.Type != RestaurantTableType.TakeAway)
            .Select(t =>
            {
                latestOrderByTable.TryGetValue(t.Id, out var order);
                var haystack = Join(t.Name, t.Section?.Name, t.Note, order is null ? null : OrderHaystack(order, null));
                return new { Table = t, Order = order, Haystack = haystack };
            })
            .Where(x => Matches(x.Haystack))
            .OrderBy(x => x.Table.RestaurantId)
            .ThenBy(x => NaturalKey(x.Table.Name), StringComparer.Ordinal)
            .Select(x => new PosSearchTableResponse
            {
                Id = x.Table.Id,
                RestaurantId = x.Table.RestaurantId,
                Name = x.Table.Name,
                Capacity = x.Table.Capacity,
                IsOccupied = x.Order is not null || x.Table.IsOccupied,
                SectionId = x.Table.SectionId,
                SectionName = x.Table.Section?.Name,
                Note = x.Table.Note,
                Type = x.Table.Type,
                ActiveOrderId = x.Order?.Id,
                OrderNumber = x.Order?.OrderNumber,
                OrderStatus = x.Order?.Status.ToString(),
                WaiterName = FullName(x.Order?.Waiter)
            })
            .ToList();

        var orders = activeOrders
            .Select(o => new { Order = o, Table = allTables.GetValueOrDefault(o.TableId) })
            .Where(x => x.Table is not null && Matches(OrderHaystack(x.Order, x.Table)))
            .Take(MaxOrders)
            .Select(x => new PosSearchOrderResponse
            {
                Id = x.Order.Id,
                OrderNumber = x.Order.OrderNumber,
                ReceiptNumber = x.Order.ReceiptNumber,
                Status = x.Order.Status.ToString(),
                TableId = x.Order.TableId,
                TableName = x.Table!.Name,
                TableType = x.Table.Type,
                IsDelivery = x.Order.IsDelivery,
                WaiterId = x.Order.WaiterId,
                WaiterName = FullName(x.Order.Waiter),
                CounterpartyName = x.Order.Counterparty?.Name,
                Note = x.Order.Note,
                DeliveryAddress = x.Order.DeliveryAddress,
                DeliveryPhone = x.Order.DeliveryPhone,
                GuestCount = x.Order.GuestCount,
                TotalAmount = x.Order.TotalAmount,
                OpenedAt = x.Order.OpenedAt
            })
            .ToList();

        return new PosSearchResponse { Tables = tables, Orders = orders };
    }

    /// <summary>"Masa 2" sorts before "Masa 10": digit runs are zero-padded before comparing.</summary>
    private static string NaturalKey(string name) =>
        Regex.Replace(SearchText.Fold(name), @"\d+", m => m.Value.PadLeft(8, '0'));
}
