using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Queries.GetReceipt;
using Application.QuickSales.Dtos;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.QuickSales.Commands.Create;

/// <summary>
/// "Tez satış" in one request: the order, its lines, the stock and the payment are written in a single
/// transaction. A quick sale never stays open, so it needs none of the table machinery — every sale of a
/// branch shares one virtual "Tez satış" table instead of creating a table per sale.
/// </summary>
public class QuickSaleCommandHandler : IRequestHandler<QuickSaleCommand, QuickSaleResponse>
{
    public const string SharedTableName = "Tez satış";

    private readonly IOrderRepository _orderRepository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICompanySettingsRepository _companySettingsRepository;
    private readonly IRecipeStockDeductionService _stockDeductionService;
    private readonly IOrderPaymentService _paymentService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly ILogger<QuickSaleCommandHandler> _logger;

    public QuickSaleCommandHandler(
        IOrderRepository orderRepository,
        IRestaurantTableRepository tableRepository,
        IEmployeeRepository employeeRepository,
        IMenuItemRepository menuItemRepository,
        ICompanySettingsRepository companySettingsRepository,
        IRecipeStockDeductionService stockDeductionService,
        IOrderPaymentService paymentService,
        ICurrentUserService currentUserService,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        ILogger<QuickSaleCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _tableRepository = tableRepository;
        _employeeRepository = employeeRepository;
        _menuItemRepository = menuItemRepository;
        _companySettingsRepository = companySettingsRepository;
        _stockDeductionService = stockDeductionService;
        _paymentService = paymentService;
        _currentUserService = currentUserService;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<QuickSaleResponse> Handle(QuickSaleCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var companyId = _currentUserService.CompanyId;

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method)
            || method is not (PaymentMethod.Cash or PaymentMethod.Card))
            throw new BadRequestException("Tez satış yalnız nağd və ya kartla ödənilə bilər.");

        var waiter = await _employeeRepository.GetByIdAsync(request.WaiterId, companyId, cancellationToken);
        if (waiter is null || !_currentUserService.CanAccessCompany(waiter.CompanyId))
            throw new BadRequestException("Garson tapılmadı.");

        if (!await _tableRepository.RestaurantExistsAsync(companyId, request.RestaurantId, cancellationToken))
            throw new BadRequestException("Filial tapılmadı.");

        // The same product tapped twice is one line.
        var wanted = request.Items
            .GroupBy(i => i.MenuItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var menuItems = (await _menuItemRepository.GetByIdsAsync(wanted.Keys.ToList(), companyId, cancellationToken))
            .ToDictionary(x => x.Id);

        foreach (var id in wanted.Keys)
        {
            if (!menuItems.TryGetValue(id, out var menuItem))
                throw new BadRequestException("MenuItem tapılmadı.");
            if (!menuItem.IsActive)
                throw new BadRequestException($"\"{menuItem.Name}\" satışda deyil.");
            if (menuItem.IsTimeBased)
                throw new BadRequestException($"\"{menuItem.Name}\" dəqiqəyə görə hesablanır — onu masa sifarişində satın.");
        }

        var settings = await _companySettingsRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        var table = await _tableRepository.GetVirtualAsync(
            companyId, request.RestaurantId, RestaurantTableType.TakeAway, SharedTableName, cancellationToken);
        if (table is null)
        {
            table = new Domain.Entities.RestaurantTable
            {
                CompanyId = companyId,
                RestaurantId = request.RestaurantId,
                Name = SharedTableName,
                Capacity = 1,
                IsActive = true,
                IsOccupied = false,
                Type = RestaurantTableType.TakeAway
            };
            await _tableRepository.AddAsync(table, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var order = new Domain.Entities.Order
        {
            CompanyId = companyId,
            OrderNumber = $"ORD-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
            RestaurantId = request.RestaurantId,
            Table = table,
            WaiterId = waiter.Id,
            // The goods change hands at once, so the sale is already "served" — the payment below closes it.
            Status = OrderStatus.Served,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            OpenedAt = now
        };

        foreach (var (menuItemId, quantity) in wanted)
        {
            var menuItem = menuItems[menuItemId];
            var line = new OrderLine
            {
                Order = order,
                MenuItemId = menuItem.Id,
                MenuItem = menuItem,
                Quantity = quantity,
                UnitPrice = OrderLinePricing.ResolveUnitPrice(menuItem, isDelivery: false, settings),
                PreparationType = menuItem.PreparationType,
                Status = menuItem.PreparationType == PreparationType.None
                    ? OrderLineStatus.Ready
                    : OrderLineStatus.Pending,
                CompanyId = companyId
            };
            line.LineTotal = OrderLinePricing.ComputeLineTotal(line);
            order.Lines.Add(line);
            await _stockDeductionService.DeductForOrderLineAsync(line, cancellationToken);

            if (menuItem.IsSet)
            {
                foreach (var component in menuItem.SetComponents)
                {
                    var child = new OrderLine
                    {
                        Order = order,
                        MenuItemId = component.ComponentMenuItemId,
                        MenuItem = component.ComponentMenuItem,
                        Quantity = component.Quantity * quantity,
                        UnitPrice = 0,
                        LineTotal = 0,
                        PreparationType = component.ComponentMenuItem.PreparationType,
                        Status = component.ComponentMenuItem.PreparationType == PreparationType.None
                            ? OrderLineStatus.Ready
                            : OrderLineStatus.Pending,
                        CompanyId = companyId,
                        ParentLine = line
                    };
                    order.Lines.Add(child);
                    await _stockDeductionService.DeductForOrderLineAsync(child, cancellationToken);
                }
            }
        }

        order.TotalAmount = order.Lines.Where(l => l.Status != OrderLineStatus.Cancelled).Sum(l => l.LineTotal);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        await _orderRepository.AddAsync(order, cancellationToken);
        await _orderRepository.SaveChangesAsync(cancellationToken);

        // Settles the whole bill: records the payment and closes the order (receipt number, paid flags).
        await _paymentService.PayAsync(
            order, null, null, method, request.PaidAmount, request.IsFiscal, request.BankId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Order",
                    EntityId = order.Id.ToString(),
                    ActionType = "Create",
                    Message = $"Tez satış. Id: {order.Id}, OrderNumber: {order.OrderNumber}, RestaurantId: {order.RestaurantId}, Məbləğ: {order.TotalAmount}, Ödəniş: {method}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(auditEx, "Quick sale audit log failed. OrderId: {OrderId}", order.Id);
        }

        _logger.LogInformation(
            "Quick sale {OrderId} paid by {Method}, total {Total}", order.Id, method, order.TotalAmount);

        var receipt = await _mediator.Send(new GetOrderReceiptQuery(order.Id), cancellationToken);

        return new QuickSaleResponse
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            ChangeAmount = order.ChangeAmount,
            Receipt = receipt
        };
    }
}
