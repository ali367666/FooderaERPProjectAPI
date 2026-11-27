using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Cash;

/// <summary>
/// Cash orders (kassa orderləri), the documents of money in and out: a receipt order (mədaxil) is a payment taken from a customer, a payment order
/// (məxaric) is a payment made to a supplier. They are read from the customer and supplier payments, so a payment taken at the till shows up here too.
/// </summary>
public record GetStoreCashOrdersQuery(
    string? Kind, DateTime? From, DateTime? To, int? RegisterId, int? PartyId, string? Code, bool IncludeReversed = true)
    : IRequest<StoreCashOrderListResponse>;

public class StoreCashOrderResponse
{
    /// <summary>"in" (mədaxil: customer payment) or "out" (məxaric: supplier payment).</summary>
    public string Kind { get; set; } = default!;

    /// <summary>The id of the customer or supplier payment, used to open or cancel it.</summary>
    public int Id { get; set; }

    public string DocumentNo { get; set; } = default!;
    public DateTime DateUtc { get; set; }

    /// <summary>The till of the shift the money went through; supplier payments are not tied to a till.</summary>
    public int? RegisterId { get; set; }
    public string? RegisterName { get; set; }

    public int PartyId { get; set; }
    public string PartyName { get; set; } = default!;
    public string? PartyCode { get; set; }

    public decimal Amount { get; set; }
    public StorePaymentMethod Method { get; set; }

    /// <summary>The basis (əsas): the reference written with the payment.</summary>
    public string? Basis { get; set; }
    public string? Note { get; set; }
    public string? AuthorName { get; set; }

    public bool IsReversed { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public string? ReverseReason { get; set; }
}

public class StoreCashOrderListResponse
{
    /// <summary>Money taken in and paid out by the orders shown (cancelled ones left out).</summary>
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public List<StoreCashOrderResponse> Items { get; set; } = new();
}

public class StoreCashOrdersHandler : IRequestHandler<GetStoreCashOrdersQuery, StoreCashOrderListResponse>
{
    private readonly IStoreCustomerRepository _customers;
    private readonly IStoreTradeRepository _trade;
    private readonly IStoreRepository _store;
    private readonly ICurrentUserService _currentUser;

    public StoreCashOrdersHandler(IStoreCustomerRepository customers, IStoreTradeRepository trade, IStoreRepository store, ICurrentUserService currentUser)
    {
        _customers = customers;
        _trade = trade;
        _store = store;
        _currentUser = currentUser;
    }

    public async Task<StoreCashOrderListResponse> Handle(GetStoreCashOrdersQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var kind = query.Kind?.Trim().ToLowerInvariant();
        if (kind is not (null or "" or "all" or "in" or "out"))
            throw new BadRequestException("Order növü 'in' (mədaxil) və ya 'out' (məxaric) olmalıdır.");

        DateTime? fromUtc = query.From is { } f ? ReportPeriodResolver.StoreFromUtc(f) : null;
        DateTime? toUtc = query.To is { } t ? ReportPeriodResolver.StoreToUtc(t) : null;

        var rows = new List<StoreCashOrderResponse>();
        var userIds = new List<int>();

        if (kind is not "out")
        {
            foreach (var p in await _customers.GetPaymentsForOrdersAsync(companyId, fromUtc, toUtc, ct))
            {
                if (query.PartyId is { } pid && p.StoreCustomerId != pid) continue;
                var register = p.StoreCashShift?.StoreCashRegister;
                if (query.RegisterId is { } rid && register?.Id != rid) continue;
                if (p.CreatedByUserId is { } u) userIds.Add(u);
                rows.Add(new StoreCashOrderResponse
                {
                    Kind = "in",
                    Id = p.Id,
                    DocumentNo = p.DocumentNo,
                    DateUtc = p.CreatedAtUtc,
                    RegisterId = register?.Id,
                    RegisterName = register?.Name,
                    PartyId = p.StoreCustomerId,
                    PartyName = p.StoreCustomer?.Name ?? string.Empty,
                    PartyCode = p.StoreCustomer?.Code,
                    Amount = p.Amount,
                    Method = p.Method,
                    Basis = p.Reference,
                    Note = p.Note,
                    IsReversed = p.ReversedAtUtc != null,
                    ReversedAtUtc = p.ReversedAtUtc,
                    ReverseReason = p.ReverseReason,
                    AuthorName = p.CreatedByUserId?.ToString(),
                });
            }
        }

        // Supplier payments are not tied to a till, so a till filter leaves them out.
        if (kind is not "in" && query.RegisterId is null)
        {
            foreach (var p in await _trade.GetSupplierPaymentsForOrdersAsync(companyId, fromUtc, toUtc, ct))
            {
                if (query.PartyId is { } pid && p.StoreSupplierId != pid) continue;
                if (p.CreatedByUserId is { } u) userIds.Add(u);
                rows.Add(new StoreCashOrderResponse
                {
                    Kind = "out",
                    Id = p.Id,
                    DocumentNo = p.DocumentNo,
                    DateUtc = p.PaymentDate,
                    PartyId = p.StoreSupplierId,
                    PartyName = p.StoreSupplier?.Name ?? string.Empty,
                    Amount = p.Amount,
                    Method = p.Method,
                    Basis = p.Reference,
                    Note = p.Note,
                    IsReversed = p.ReversedAtUtc != null,
                    ReversedAtUtc = p.ReversedAtUtc,
                    ReverseReason = p.ReverseReason,
                    AuthorName = p.CreatedByUserId?.ToString(),
                });
            }
        }

        var names = await _store.GetUserNamesAsync(userIds, ct);
        foreach (var r in rows)
            r.AuthorName = int.TryParse(r.AuthorName, out var id) && names.TryGetValue(id, out var n) ? n : null;

        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            var code = query.Code.Trim();
            rows = rows.Where(r => r.DocumentNo.Contains(code, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (!query.IncludeReversed)
            rows = rows.Where(r => !r.IsReversed).ToList();

        rows = rows.OrderByDescending(r => r.DateUtc).ToList();
        return new StoreCashOrderListResponse
        {
            TotalIn = rows.Where(r => r.Kind == "in" && !r.IsReversed).Sum(r => r.Amount),
            TotalOut = rows.Where(r => r.Kind == "out" && !r.IsReversed).Sum(r => r.Amount),
            Items = rows,
        };
    }
}
