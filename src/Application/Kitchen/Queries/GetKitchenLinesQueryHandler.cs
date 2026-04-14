using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Kitchen.Dtos;
using MediatR;

namespace Application.Kitchen.Queries.GetKitchenLines;

public class GetKitchenLinesQueryHandler
    : IRequestHandler<GetKitchenLinesQuery, List<KitchenOrderLineResponse>>
{
    private readonly IOrderLineRepository _orderLineRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetKitchenLinesQueryHandler(
        IOrderLineRepository orderLineRepository,
        ICurrentUserService currentUserService)
    {
        _orderLineRepository = orderLineRepository;
        _currentUserService = currentUserService;
    }

    public async Task<List<KitchenOrderLineResponse>> Handle(
        GetKitchenLinesQuery request,
        CancellationToken cancellationToken)
    {
        var lines = await _orderLineRepository.GetKitchenLinesAsync(
            _currentUserService.CompanyId,
            request.RestaurantId,
            cancellationToken);

        return lines.Select(x => new KitchenOrderLineResponse
        {
            OrderLineId = x.Id,
            OrderId = x.OrderId,
            OrderNumber = x.Order.OrderNumber,
            TableId = x.Order.TableId,
            TableName = x.Order.Table.Name,
            MenuItemId = x.MenuItemId,
            MenuItemName = x.MenuItem.Name,
            Quantity = x.Quantity,
            Note = x.Note,
            Status = x.Status,
            OpenedAt = x.Order.OpenedAt
        }).ToList();
    }
}