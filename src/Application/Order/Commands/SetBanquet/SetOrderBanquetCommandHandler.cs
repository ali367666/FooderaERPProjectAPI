using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Dtos;
using Application.Orders.Queries.GetById;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.SetBanquet;

public class SetOrderBanquetCommandHandler : IRequestHandler<SetOrderBanquetCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public SetOrderBanquetCommandHandler(
        IOrderRepository orderRepository,
        IRestaurantTableRepository tableRepository,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<OrderResponse> Handle(SetOrderBanquetCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithLinesAsync(
            request.OrderId, _currentUserService.CompanyId, cancellationToken);
        if (order is null)
            throw new NotFoundException("Sifariş tapılmadı.");

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.Cancelled)
            throw new BadRequestException("Bu sifariş dəyişdirilə bilməz.");

        if (order.IsBanquet != request.Banquet)
        {
            if (!request.Banquet)
            {
                var table = await _tableRepository.GetByIdAsync(order.TableId, _currentUserService.CompanyId, cancellationToken);
                if (table?.Section?.IsBanquet == true)
                    throw new BadRequestException("Banket zalının sifarişi adi menyuya keçirilə bilməz.");
            }

            if (order.Lines.Any(l => l.Status != OrderLineStatus.Cancelled))
                throw new BadRequestException(
                    "Banket menyusuna keçmək (və ya qayıtmaq) üçün əvvəlcə sifarişdəki məhsulları silin.");

            order.IsBanquet = request.Banquet;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync(cancellationToken);
        }

        return await _mediator.Send(new GetOrderByIdQuery(order.Id), cancellationToken);
    }
}
