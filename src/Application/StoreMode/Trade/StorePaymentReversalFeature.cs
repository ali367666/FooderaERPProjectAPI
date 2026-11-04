using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Customers;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Trade;

public class ReverseStorePaymentRequest
{
    public string Reason { get; set; } = default!;
}

/// <summary>Cancels a customer's debt payment by a counter entry (the payment itself stays, marked reversed). Only while its shift is open.</summary>
public record ReverseStoreCustomerPaymentCommand(int PaymentId, ReverseStorePaymentRequest Request) : IRequest<StoreCustomerStatementResponse>;

/// <summary>Cancels a payment made to a supplier by a counter entry; the purchases it paid become unpaid again.</summary>
public record ReverseStoreSupplierPaymentCommand(int PaymentId, ReverseStorePaymentRequest Request) : IRequest<StoreSupplierPaymentResponse>;

public class StorePaymentReversalHandlers :
    IRequestHandler<ReverseStoreCustomerPaymentCommand, StoreCustomerStatementResponse>,
    IRequestHandler<ReverseStoreSupplierPaymentCommand, StoreSupplierPaymentResponse>
{
    private readonly IStoreCustomerRepository _customers;
    private readonly IStoreTradeRepository _trade;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public StorePaymentReversalHandlers(
        IStoreCustomerRepository customers,
        IStoreTradeRepository trade,
        IStoreRepository store,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _customers = customers;
        _trade = trade;
        _store = store;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    private static string RequireReason(ReverseStorePaymentRequest request)
        => string.IsNullOrWhiteSpace(request.Reason) ? throw new BadRequestException("Ləğv səbəbini yazın.") : request.Reason.Trim();

    public async Task<StoreCustomerStatementResponse> Handle(ReverseStoreCustomerPaymentCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var reason = RequireReason(command.Request);

        var payment = await _customers.GetCustomerPaymentAsync(command.PaymentId, companyId, ct)
            ?? throw new NotFoundException("Ödəniş tapılmadı.");
        if (payment.ReversedAtUtc is not null)
            throw new BadRequestException("Bu ödəniş artıq ləğv edilib.");

        var shift = await _store.GetShiftAsync(payment.StoreCashShiftId, companyId, ct);
        if (shift is null || shift.Status != StoreShiftStatus.Open)
            throw new BadRequestException("Ödənişin növbəsi bağlanıb; ləğv etmək olmur.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            payment.ReversedAtUtc = DateTime.UtcNow;
            payment.ReversedByUserId = _currentUser.UserId;
            payment.ReverseReason = reason;

            await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
            {
                CompanyId = companyId,
                PartyType = StoreLedgerPartyType.Customer,
                PartyId = payment.StoreCustomerId,
                Type = StoreLedgerEntryType.PaymentReversal,
                Amount = payment.Amount,
                DocumentType = "CustomerPayment",
                DocumentId = payment.Id,
                DocumentNo = payment.DocumentNo,
                Note = $"Ödənişin ləğvi: {reason}",
                CreatedByUserId = _currentUser.UserId,
            }, ct);
            await _customers.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return await _mediator.Send(new GetStoreCustomerStatementQuery(payment.StoreCustomerId, null, null), ct);
    }

    public async Task<StoreSupplierPaymentResponse> Handle(ReverseStoreSupplierPaymentCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var reason = RequireReason(command.Request);

        var payment = await _trade.GetSupplierPaymentAsync(command.PaymentId, companyId, ct)
            ?? throw new NotFoundException("Ödəniş tapılmadı.");
        if (payment.ReversedAtUtc is not null)
            throw new BadRequestException("Bu ödəniş artıq ləğv edilib.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            payment.ReversedAtUtc = DateTime.UtcNow;
            payment.ReversedByUserId = _currentUser.UserId;
            payment.ReverseReason = reason;

            await _trade.AddLedgerEntryAsync(new StoreLedgerEntry
            {
                CompanyId = companyId,
                PartyType = StoreLedgerPartyType.Supplier,
                PartyId = payment.StoreSupplierId,
                Type = StoreLedgerEntryType.PaymentReversal,
                Amount = payment.Amount,
                DocumentType = "SupplierPayment",
                DocumentId = payment.Id,
                DocumentNo = payment.DocumentNo,
                Note = $"Ödənişin ləğvi: {reason}",
                CreatedByUserId = _currentUser.UserId,
            }, ct);
            await _trade.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        var payments = await _trade.GetSupplierPaymentsAsync(companyId, payment.StoreSupplierId, ct);
        return StorePaymentService.Map(payments.First(p => p.Id == payment.Id));
    }
}
