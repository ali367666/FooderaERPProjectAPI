using System.Text.Json;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Orders.Dtos;
using Application.Orders.Services;
using Domain.Enums;
using MediatR;

namespace Application.Orders.Commands.EditReceipt;

public class ChangeReceiptPaymentRequest
{
    /// <summary>Which payment of the receipt; empty when the receipt has only one (or none).</summary>
    public int? PaymentId { get; set; }

    /// <summary>"Cash" or "Card".</summary>
    public string Method { get; set; } = default!;

    /// <summary>The bank of a card payment (required for Card).</summary>
    public int? BankId { get; set; }

    /// <summary>Why the receipt is corrected - kept in the audit log.</summary>
    public string Reason { get; set; } = default!;
}

public class EditReceiptLineRequest
{
    /// <summary>The new quantity of the line (0 removes it; the receipt keeps at least one item).</summary>
    public int Quantity { get; set; }
    public string Reason { get; set; } = default!;
}

public record ChangeReceiptPaymentCommand(int OrderId, ChangeReceiptPaymentRequest Request) : IRequest<ReceiptEditResponse>;
public record EditReceiptLineCommand(int OrderId, int LineId, EditReceiptLineRequest Request) : IRequest<ReceiptEditResponse>;

public class ReceiptEditResponse
{
    public int OrderId { get; set; }
    public string? ReceiptNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
}

internal static class ReceiptEditGuards
{
    public static async Task<Domain.Entities.Order> LoadEditableAsync(
        IOrderRepository orders, ICompanySettingsRepository settings, ICurrentUserService user, int orderId, string reason, CancellationToken ct)
    {
        var order = await orders.GetByIdWithLinesAsync(orderId, ct);
        if (order is null || !user.CanAccessCompany(order.CompanyId))
            throw new NotFoundException("Qəbz tapılmadı.");

        if ((await settings.GetByCompanyIdAsync(order.CompanyId, ct))?.ReceiptEditAllowed != true)
            throw new BadRequestException("Qəbz düzəlişi bu şirkətdə bağlıdır.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new BadRequestException("Düzəlişin səbəbini yazın.");
        if (!order.IsPaid || order.Status != OrderStatus.Paid)
            throw new BadRequestException("Yalnız ödənilmiş qəbz düzəldilə bilər.");
        return order;
    }

    public static ReceiptEditResponse Map(Domain.Entities.Order o) => new()
    {
        OrderId = o.Id,
        ReceiptNumber = o.ReceiptNumber,
        TotalAmount = o.TotalAmount,
        PaymentMethod = o.PaymentMethod?.ToString(),
        PaidAmount = o.PaidAmount,
        ChangeAmount = o.ChangeAmount,
    };
}

public class ChangeReceiptPaymentCommandHandler : IRequestHandler<ChangeReceiptPaymentCommand, ReceiptEditResponse>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderPaymentRepository _payments;
    private readonly ICompanySettingsRepository _settings;
    private readonly ICounterpartyRepository _counterparties;
    private readonly ICurrentUserService _user;
    private readonly IAuditLogService _audit;

    public ChangeReceiptPaymentCommandHandler(
        IOrderRepository orders, IOrderPaymentRepository payments, ICompanySettingsRepository settings,
        ICounterpartyRepository counterparties, ICurrentUserService user, IAuditLogService audit)
    {
        _orders = orders;
        _payments = payments;
        _settings = settings;
        _counterparties = counterparties;
        _user = user;
        _audit = audit;
    }

    public async Task<ReceiptEditResponse> Handle(ChangeReceiptPaymentCommand command, CancellationToken ct)
    {
        var dto = command.Request;
        var order = await ReceiptEditGuards.LoadEditableAsync(_orders, _settings, _user, command.OrderId, dto.Reason, ct);

        if (!Enum.TryParse<PaymentMethod>(dto.Method, true, out var method) || method is not (PaymentMethod.Cash or PaymentMethod.Card))
            throw new BadRequestException("Ödəniş üsulu nağd və ya kart olmalıdır.");

        var payments = await _payments.GetByOrderIdAsync(order.Id, ct);
        Domain.Entities.OrderPayment? target = null;
        if (payments.Count > 0)
        {
            target = dto.PaymentId is { } pid ? payments.FirstOrDefault(p => p.Id == pid) : (payments.Count == 1 ? payments[0] : null);
            if (target is null)
                throw new BadRequestException(payments.Count > 1 ? "Qəbzdə bir neçə ödəniş var, hansını dəyişəcəyinizi seçin." : "Ödəniş tapılmadı.");
            if (target.Method == PaymentMethod.Credit)
                throw new BadRequestException("Borca yazılmış ödəniş dəyişdirilə bilməz.");
        }
        else if (order.PaymentMethod == PaymentMethod.Credit)
        {
            throw new BadRequestException("Borca yazılmış qəbz dəyişdirilə bilməz.");
        }

        var bank = method == PaymentMethod.Card
            ? await PaymentBankResolver.RequireAsync(_counterparties, order.CompanyId, dto.BankId, ct)
            : null;

        var oldMethod = target?.Method ?? order.PaymentMethod;
        var oldBank = target?.BankCounterpartyId ?? order.BankCounterpartyId;
        ReceiptEditor.ApplyPaymentMethod(order, payments, target, method, bank);

        _orders.Update(order);
        await _orders.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLogEntry
        {
            EntityName = "Order",
            EntityId = order.Id.ToString(),
            ActionType = "ReceiptPaymentChanged",
            OldValues = JsonSerializer.Serialize(new { Method = oldMethod?.ToString(), BankId = oldBank }),
            NewValues = JsonSerializer.Serialize(new { Method = method.ToString(), BankId = bank?.Id, PaymentId = target?.Id }),
            Message = $"Qəbz {order.ReceiptNumber}: ödəniş üsulu {oldMethod} → {method}. Səbəb: {dto.Reason.Trim()}",
            UserId = _user.UserId,
            CompanyId = order.CompanyId,
        }, ct);

        return ReceiptEditGuards.Map(order);
    }
}

public class EditReceiptLineCommandHandler : IRequestHandler<EditReceiptLineCommand, ReceiptEditResponse>
{
    private readonly IOrderRepository _orders;
    private readonly IOrderPaymentRepository _payments;
    private readonly ICompanySettingsRepository _settings;
    private readonly IRecipeStockDeductionService _stock;
    private readonly ICurrentUserService _user;
    private readonly IAuditLogService _audit;

    public EditReceiptLineCommandHandler(
        IOrderRepository orders, IOrderPaymentRepository payments, ICompanySettingsRepository settings,
        IRecipeStockDeductionService stock, ICurrentUserService user, IAuditLogService audit)
    {
        _orders = orders;
        _payments = payments;
        _settings = settings;
        _stock = stock;
        _user = user;
        _audit = audit;
    }

    public async Task<ReceiptEditResponse> Handle(EditReceiptLineCommand command, CancellationToken ct)
    {
        var dto = command.Request;
        var order = await ReceiptEditGuards.LoadEditableAsync(_orders, _settings, _user, command.OrderId, dto.Reason, ct);

        if (order.IsFiscal)
            throw new BadRequestException("Fiskal çekin məhsulları dəyişdirilə bilməz.");
        if (dto.Quantity < 0)
            throw new BadRequestException("Say mənfi ola bilməz.");

        var line = order.Lines.DistinctBy(l => l.Id).FirstOrDefault(l => l.Id == command.LineId && l.Status != OrderLineStatus.Cancelled)
            ?? throw new NotFoundException("Qəbzdə belə məhsul yoxdur.");
        if (line.ParentLineId is not null || line.MenuItem.IsSet)
            throw new BadRequestException("Set və onun tərkibi dəyişdirilə bilməz.");
        if (line.MenuItem.IsTimeBased)
            throw new BadRequestException("Vaxtla hesablanan məhsulun sayı dəyişdirilə bilməz.");
        if (dto.Quantity == line.Quantity)
            throw new BadRequestException("Say dəyişməyib.");
        if (dto.Quantity == 0 && order.Lines.DistinctBy(l => l.Id).Count(l => l.Status != OrderLineStatus.Cancelled && l.ParentLineId == null) <= 1)
            throw new BadRequestException("Qəbzdə ən azı bir məhsul qalmalıdır.");

        var payments = await _payments.GetByOrderIdAsync(order.Id, ct);
        if (payments.Count > 1)
            throw new BadRequestException("Bir neçə ödənişlə bağlanmış qəbzin məhsulları dəyişdirilə bilməz.");
        if (payments.Any(p => p.Method == PaymentMethod.Credit) || order.PaymentMethod == PaymentMethod.Credit)
            throw new BadRequestException("Borca yazılmış qəbzin məhsulları dəyişdirilə bilməz.");

        var oldQuantity = line.Quantity;
        var oldTotal = order.TotalAmount;

        // The stock follows the new quantity: what was taken off for the old quantity is given back and the new quantity is taken.
        var wasDeducted = line.IsStockDeducted;
        if (wasDeducted)
            await _stock.RestoreForOrderLineAsync(line, ct);
        ReceiptEditor.ApplyLineQuantity(order, line, dto.Quantity, payments);
        if (wasDeducted && dto.Quantity > 0)
            await _stock.DeductForOrderLineAsync(line, ct);

        _orders.Update(order);
        await _orders.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLogEntry
        {
            EntityName = "Order",
            EntityId = order.Id.ToString(),
            ActionType = "ReceiptItemsChanged",
            OldValues = JsonSerializer.Serialize(new { LineId = line.Id, Quantity = oldQuantity, Total = oldTotal }),
            NewValues = JsonSerializer.Serialize(new { LineId = line.Id, Quantity = dto.Quantity, Total = order.TotalAmount }),
            Message = $"Qəbz {order.ReceiptNumber}: {line.MenuItem.Name} {oldQuantity} → {dto.Quantity}, cəmi {oldTotal:0.00} → {order.TotalAmount:0.00}. Səbəb: {dto.Reason.Trim()}",
            UserId = _user.UserId,
            CompanyId = order.CompanyId,
        }, ct);

        return ReceiptEditGuards.Map(order);
    }
}
