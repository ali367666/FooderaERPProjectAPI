using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Orders.Dtos;
using Application.Orders.Dtos.Request;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.Pay;

public class PayOrderCommandHandler : IRequestHandler<PayOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRecipeStockDeductionService _recipeStockDeductionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrderPaymentRepository _paymentRepository;
    private readonly IOrderPaymentService _paymentService;

    public PayOrderCommandHandler(
        IOrderRepository orderRepository,
        IRecipeStockDeductionService recipeStockDeductionService,
        ICurrentUserService currentUserService,
        IOrderPaymentRepository paymentRepository,
        IOrderPaymentService paymentService)
    {
        _orderRepository = orderRepository;
        _recipeStockDeductionService = recipeStockDeductionService;
        _currentUserService = currentUserService;
        _paymentRepository = paymentRepository;
        _paymentService = paymentService;
    }

    public async Task<OrderResponse> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithLinesAsync(request.OrderId, cancellationToken);
        if (order is null || !_currentUserService.CanAccessCompany(order.CompanyId))
            throw new NotFoundException("Order not found.");

        if (order.IsPaid || order.Status == OrderStatus.Paid)
            throw new BadRequestException("This order is already paid.");

        if (order.Status != OrderStatus.Ready && order.Status != OrderStatus.Served)
            throw new BadRequestException("Only ready or served orders can be paid.");

        if (order.TableRentalStartedAt is not null && order.TableRentalStoppedAt is null)
            throw new BadRequestException("Masa icarəsi taymeri hələ işləyir — əvvəlcə dayandırın.");

        if (!Enum.TryParse<PaymentMethod>(request.Request.PaymentMethod, true, out var paymentMethod))
            throw new BadRequestException("Please select a valid payment method.");

        // Some guests already paid their share: this payment settles whatever is left.
        if (await _paymentRepository.AnyForOrderAsync(order.Id, cancellationToken))
        {
            await _paymentService.PayAsync(
                order, null, null, paymentMethod, request.Request.PaidAmount, request.Request.IsFiscal, cancellationToken);
            return BuildResponse(order);
        }

        foreach (var line in order.Lines)
            line.LineTotal = OrderLinePricing.ComputeLineTotal(line);

        var subtotal = order.Lines
            .DistinctBy(x => x.Id)
            .Where(x => x.Status != OrderLineStatus.Cancelled)
            .Sum(x => x.LineTotal);

        // Discount (if any) was locked in by ApplyDiscountToOrderCommand before payment
        // The service charge recorded on the order ("Servis haqqı qeyd etmək").
        var serviceCharge = order.ServiceChargeAmount is > 0 ? order.ServiceChargeAmount : null;
        var totalAmount = Math.Max(0, subtotal - order.DiscountAmount + (serviceCharge ?? 0) + (order.TableRentalAmount ?? 0));

        if (paymentMethod == PaymentMethod.Credit)
        {
            if (order.CounterpartyId is null || order.Counterparty is null)
                throw new BadRequestException("Borca yazmaq üçün əvvəlcə müştəri seçilməlidir.");
        }
        else if (request.Request.PaidAmount < totalAmount)
        {
            throw new BadRequestException("Paid amount cannot be less than total amount.");
        }

        var nonKitchenLines = order.Lines
            .Where(x =>
                x.Status != OrderLineStatus.Cancelled &&
                (x.MenuItem?.PreparationType ?? x.PreparationType) != PreparationType.Kitchen)
            .ToList();
        foreach (var line in nonKitchenLines.Where(x => !x.IsStockDeducted))
        {
            await _recipeStockDeductionService.DeductForOrderLineAsync(line, cancellationToken);
        }

        order.TotalAmount = totalAmount;
        order.ServiceChargeAmount = serviceCharge;
        order.IsPaid = true;
        order.PaidAt = DateTime.UtcNow;
        order.PaymentMethod = paymentMethod;
        order.IsFiscal = request.Request.IsFiscal;
        order.PaidAmount = paymentMethod == PaymentMethod.Credit ? 0 : request.Request.PaidAmount;
        order.ChangeAmount = paymentMethod == PaymentMethod.Credit ? 0 : request.Request.PaidAmount - totalAmount;
        order.Status = OrderStatus.Paid;
        if (paymentMethod == PaymentMethod.Credit)
            order.Counterparty!.CurrentDebtAmount += totalAmount;
        order.ReceiptNumber = $"RCPT-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{order.Id}";
        order.ClosedAt = order.PaidAt;
        if (order.Table is not null)
            order.Table.IsOccupied = false;

        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        return BuildResponse(order);
    }

    private static OrderResponse BuildResponse(Domain.Entities.Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CompanyId = order.CompanyId,
            OrderNumber = order.OrderNumber,
            RestaurantId = order.RestaurantId,
            RestaurantName = order.Restaurant?.Name,
            TableId = order.TableId,
            TableName = order.Table?.Name,
            WaiterId = order.WaiterId,
            WaiterName = order.Waiter != null ? $"{order.Waiter.FirstName} {order.Waiter.LastName}" : null,
            ProcessedByUserId = order.ProcessedByUserId,
            ProcessedByUserName = order.ProcessedByUser?.FullName,
            ProcessedAt = order.ProcessedAt,
            Status = order.Status.ToString(),
            Note = order.Note,
            GuestCount = order.GuestCount,
            CounterpartyId = order.CounterpartyId,
            CounterpartyName = order.Counterparty?.Name,
            CounterpartyDebtAmount = order.Counterparty?.CurrentDebtAmount,
            OpenedAt = order.OpenedAt,
            ClosedAt = order.ClosedAt,
            TotalAmount = order.TotalAmount,
            DiscountCode = order.DiscountCode,
            DiscountAmount = order.DiscountAmount,
            ServiceChargeAmount = order.ServiceChargeAmount,
            IsPaid = order.IsPaid,
            PaidAt = order.PaidAt,
            PaymentMethod = order.PaymentMethod?.ToString(),
            PaidAmount = order.PaidAmount,
            ChangeAmount = order.ChangeAmount,
            ReceiptNumber = order.ReceiptNumber,
            TableHourlyRate = order.Table?.HourlyRate,
            TableRentalStartedAt = order.TableRentalStartedAt,
            TableRentalStoppedAt = order.TableRentalStoppedAt,
            TableRentalAmount = order.TableRentalAmount,
            HoldUntilUtc = order.HoldUntilUtc,
            BillPrintedAt = order.BillPrintedAt,
            IsBillLocked = order.IsBillLocked,
            IsDelivery = order.IsDelivery,
            DeliveryAddress = order.DeliveryAddress,
            DeliveryPhone = order.DeliveryPhone,
            DeliveryDriverEmployeeId = order.DeliveryDriverEmployeeId,
            DeliveryDriverName = order.DeliveryDriverEmployee != null ? $"{order.DeliveryDriverEmployee.FirstName} {order.DeliveryDriverEmployee.LastName}" : null,
            Lines = order.Lines.DistinctBy(x => x.Id).Select(x => new OrderLineResponse
            {
                Id = x.Id,
                MenuItemId = x.MenuItemId,
                MenuItemName = x.MenuItem.Name,
                MenuItemType = x.MenuItem.PreparationType.ToString(),
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                LineTotal = x.LineTotal,
                HoldUntilUtc = x.HoldUntilUtc,
                KitchenPrintedAt = x.KitchenPrintedAt,
                TimeBasedStartedAt = x.TimeBasedStartedAt,
                TimeBasedStoppedAt = x.TimeBasedStoppedAt,
                IsTimeBased = x.MenuItem.IsTimeBased,
                IsWeightBased = Application.Common.Helpers.OrderLinePricing.IsWeightBased(x.MenuItem.UnitId),
                IsGift = x.IsGift,
                DiscountAmount = x.DiscountAmount,
                PreparationType = x.PreparationType,
                Status = x.Status.ToString(),
                Note = x.Note
            }).ToList()
        };
    }
}
