using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Kitchen.Dtos;
using MediatR;

namespace Application.Kitchen.Queries;

public class GetKitchenOrdersQueryHandler : IRequestHandler<GetKitchenOrdersQuery, List<KitchenOrderLineResponse>>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetKitchenOrdersQueryHandler(
        IOrderLineRepository orderLineRepository,
        ICurrentUserService currentUserService)
    {
        _orderLineRepository = orderLineRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<KitchenOrderLineResponse>> Handle(
        GetKitchenOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = request.CompanyId.GetValueOrDefault() > 0
            ? request.CompanyId!.Value
            : _currentUserService.CompanyId;

        var lines = await _orderLineRepository.GetKitchenLinesByCompanyAsync(
            companyId,
            cancellationToken);

        return lines.Select(x => new KitchenOrderLineResponse
            {
                OrderLineId = x.Id,
                OrderId = x.OrderId,
                OrderNumber = x.Order.OrderNumber,
                TableName = x.Order.Table.Name,
                RestaurantName = x.Order.Restaurant.Name,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                Quantity = x.Quantity,
                Note = x.Note,
                OrderStatus = x.Order.Status,
                KitchenStatus = x.Status,
                CreatedAt = x.CreatedAtUtc
            })
            .OrderBy(x => x.CreatedAt)
            .ToList();
    }
}
