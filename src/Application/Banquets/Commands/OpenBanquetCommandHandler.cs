using Application.Banquets.Services;
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Orders.Commands.Create;
using Application.Orders.Dtos;
using Application.Orders.Queries.GetById;
using Domain.Entities;
using MediatR;

namespace Application.Banquets.Commands;

public class OpenBanquetCommandHandler : IRequestHandler<OpenBanquetCommand, OrderResponse>
{
    private readonly IBanquetTableChecker _checker;
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public OpenBanquetCommandHandler(
        IBanquetTableChecker checker,
        IOrderRepository orderRepository,
        ICurrentUserService currentUserService,
        IMediator mediator)
    {
        _checker = checker;
        _orderRepository = orderRepository;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task<OrderResponse> Handle(OpenBanquetCommand command, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = command.Request;

        var tables = await _checker.LoadTablesAsync(companyId, dto.RestaurantId, dto.TableIds, cancellationToken);
        foreach (var table in tables)
        {
            var busy = await _checker.GetBusyReasonNowAsync(companyId, table, cancellationToken);
            if (busy is not null)
                throw new BadRequestException($"{table.Name}: {busy}");
        }

        // The ordinary create path opens the order on the first table (waiter, order number, audit log...).
        var created = await _mediator.Send(new CreateOrderCommand(new CreateOrderRequest
        {
            RestaurantId = dto.RestaurantId,
            TableId = tables[0].Id,
            WaiterId = dto.WaiterId,
            GuestCount = dto.GuestCount,
            Note = dto.Note,
            IsBanquet = true
        }), cancellationToken);

        var order = await _orderRepository.GetByIdAsync(created.Id, companyId, cancellationToken)
            ?? throw new NotFoundException("Sifariş tapılmadı.");

        foreach (var table in tables)
            order.BanquetTables.Add(new OrderTable { CompanyId = companyId, OrderId = order.Id, TableId = table.Id });

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetOrderByIdQuery(order.Id), cancellationToken);
    }
}
