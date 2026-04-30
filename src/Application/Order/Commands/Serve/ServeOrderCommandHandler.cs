using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;

namespace Application.Order.Commands.Serve;

public class ServeOrderCommandHandler : IRequestHandler<ServeOrderCommand, BaseResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;

    public ServeOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse> Handle(ServeOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithLinesAsync(
            request.OrderId,
            _currentUserService.CompanyId,
            cancellationToken);

        if (order is null)
            return BaseResponse.Fail("Order not found.");

        if (order.Status != OrderStatus.Ready)
            return BaseResponse.Fail("Only ready orders can be served.");

        order.Status = OrderStatus.Served;

        foreach (var line in order.Lines.Where(x => x.Status == OrderLineStatus.Ready))
            line.Status = OrderLineStatus.Served;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Order served successfully.");
    }
}
