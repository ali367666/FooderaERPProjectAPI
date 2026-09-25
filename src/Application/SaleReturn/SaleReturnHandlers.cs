using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Domain.Enums;
using MediatR;

namespace Application.SaleReturn;

public record FindReturnableOrderQuery(string Code) : IRequest<ReturnableOrderResponse>;

public record CreateSaleReturnCommand(CreateSaleReturnRequest Request) : IRequest<SaleReturnResponse>;

public record GetSaleReturnsQuery(int RestaurantId, DateTime From, DateTime To) : IRequest<List<SaleReturnResponse>>;

public class FindReturnableOrderQueryHandler : IRequestHandler<FindReturnableOrderQuery, ReturnableOrderResponse>
{
    private readonly ISaleReturnRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public FindReturnableOrderQueryHandler(ISaleReturnRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<ReturnableOrderResponse> Handle(FindReturnableOrderQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BadRequestException("Qəbz barkodunu oxudun.");

        var companyId = _currentUserService.CompanyId;
        var order = await _repository.FindPaidOrderForReturnAsync(companyId, request.Code, cancellationToken);
        if (order is null)
            throw new NotFoundException("Bu barkodla ödənilmiş satış tapılmadı.");

        var returns = await _repository.GetByOrderIdAsync(companyId, order.Id, cancellationToken);
        return SaleReturnMapping.MapOrder(order, returns);
    }
}

public class CreateSaleReturnCommandHandler : IRequestHandler<CreateSaleReturnCommand, SaleReturnResponse>
{
    private readonly ISaleReturnRepository _repository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICashMovementRepository _cashMovementRepository;
    private readonly IRecipeStockDeductionService _recipeStockDeductionService;
    private readonly ICurrentUserService _currentUserService;

    public CreateSaleReturnCommandHandler(
        ISaleReturnRepository repository,
        IOrderRepository orderRepository,
        ICashMovementRepository cashMovementRepository,
        IRecipeStockDeductionService recipeStockDeductionService,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _orderRepository = orderRepository;
        _cashMovementRepository = cashMovementRepository;
        _recipeStockDeductionService = recipeStockDeductionService;
        _currentUserService = currentUserService;
    }

    public async Task<SaleReturnResponse> Handle(CreateSaleReturnCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var requested = dto.Lines.Where(x => x.Quantity > 0).ToList();
        if (requested.Count == 0)
            throw new BadRequestException("Qaytarmaq üçün ən azı bir məhsul seçin.");
        if (requested.Select(x => x.OrderLineId).Distinct().Count() != requested.Count)
            throw new BadRequestException("Eyni məhsul iki dəfə seçilib.");

        var order = await _orderRepository.GetByIdAsync(dto.OrderId, companyId, cancellationToken);
        if (order is null || order.Status != OrderStatus.Paid || order.PaymentMethod is null)
            throw new NotFoundException("Ödənilmiş satış tapılmadı.");

        var factor = SaleReturnMapping.PaidFactor(order);
        var linesById = SaleReturnMapping.ActiveLines(order).ToDictionary(x => x.Id);
        var now = DateTime.UtcNow;

        var saleReturn = new Domain.Entities.SaleReturn
        {
            CompanyId = companyId,
            RestaurantId = order.RestaurantId,
            OrderId = order.Id,
            ReturnNumber = $"RET-{now:yyyyMMddHHmmssfff}-{order.Id}",
            PaymentMethod = order.PaymentMethod.Value,
            RestockItems = dto.RestockItems,
            Reason = string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),
            CreatedByUserId = _currentUserService.UserId,
            CreatedAtUtc = now
        };

        foreach (var item in requested)
        {
            if (!linesById.TryGetValue(item.OrderLineId, out var line))
                throw new BadRequestException("Seçilmiş məhsul bu satışa aid deyil.");

            var returnable = line.Quantity - line.ReturnedQuantity;
            if (item.Quantity > returnable)
                throw new BadRequestException($"{line.MenuItem?.Name}: ən çox {returnable} qaytarmaq olar.");

            // The last return of a line takes whatever is left, so rounding never leaves cents behind.
            var unitAmount = SaleReturnMapping.RefundUnitAmount(line, factor);
            var amount = item.Quantity == returnable
                ? Math.Round(line.LineTotal * factor, 2) - Math.Round(unitAmount * line.ReturnedQuantity, 2)
                : Math.Round(unitAmount * item.Quantity, 2);

            if (dto.RestockItems)
                await _recipeStockDeductionService.RestoreForReturnAsync(
                    order, line, item.Quantity, saleReturn.ReturnNumber, cancellationToken);

            line.ReturnedQuantity += item.Quantity;

            saleReturn.Lines.Add(new Domain.Entities.SaleReturnLine
            {
                OrderLineId = line.Id,
                MenuItemId = line.MenuItemId,
                MenuItem = line.MenuItem!,
                Quantity = item.Quantity,
                Amount = Math.Max(0, amount),
                CreatedByUserId = _currentUserService.UserId,
                CreatedAtUtc = now
            });
        }

        saleReturn.TotalAmount = saleReturn.Lines.Sum(x => x.Amount);

        switch (saleReturn.PaymentMethod)
        {
            // Cash goes back out of the drawer — recorded so the Kassa balance stays right.
            case PaymentMethod.Cash when saleReturn.TotalAmount > 0:
                await _cashMovementRepository.AddAsync(new Domain.Entities.CashMovement
                {
                    CompanyId = companyId,
                    RestaurantId = order.RestaurantId,
                    Type = CashMovementType.Withdrawal,
                    Amount = saleReturn.TotalAmount,
                    Reason = $"Geri qaytarma {saleReturn.ReturnNumber} (sifariş {order.OrderNumber})",
                    CreatedByUserId = _currentUserService.UserId,
                    CreatedAtUtc = now
                }, cancellationToken);
                break;
            // A sale written to the customer's debt is refunded by reducing that debt.
            case PaymentMethod.Credit when order.Counterparty is not null:
                order.Counterparty.CurrentDebtAmount -= saleReturn.TotalAmount;
                break;
        }

        _orderRepository.Update(order);
        await _repository.AddAsync(saleReturn, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        saleReturn.Order = order;
        return SaleReturnMapping.MapReturn(saleReturn);
    }
}

public class GetSaleReturnsQueryHandler : IRequestHandler<GetSaleReturnsQuery, List<SaleReturnResponse>>
{
    private readonly ISaleReturnRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetSaleReturnsQueryHandler(ISaleReturnRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<List<SaleReturnResponse>> Handle(GetSaleReturnsQuery request, CancellationToken cancellationToken)
    {
        var returns = await _repository.GetBetweenAsync(
            _currentUserService.CompanyId, request.RestaurantId, request.From, request.To, cancellationToken);
        return returns.Select(SaleReturnMapping.MapReturn).ToList();
    }
}
