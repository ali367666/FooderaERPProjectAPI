using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Cash;

/// <summary>
/// The shift document (növbə sənədi): one shift as a document with everything that happened in it - what was sold and how many,
/// every sale, cancelled sale, return, cash movement and manager approval (removed cart lines, discounts, price changes).
/// </summary>
public record GetStoreShiftDocumentQuery(int ShiftId) : IRequest<StoreShiftDocumentResponse>;

public class StoreShiftDocumentProductResponse
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Barcode { get; set; }
    public decimal SoldQuantity { get; set; }
    public decimal SoldAmount { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal ReturnedAmount { get; set; }
}

public class StoreShiftDocumentSaleResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public StoreSaleStatus Status { get; set; }
    public string? CustomerName { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Total { get; set; }
    public List<StorePaymentTotalResponse> Payments { get; set; } = new();
    public List<StoreSaleLineResponse> Lines { get; set; } = new();
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancelledByName { get; set; }
    public string? CancelReason { get; set; }
    public bool IsReceiptless { get; set; }
}

public class StoreShiftDocumentReturnResponse
{
    public int Id { get; set; }
    public string DocumentNo { get; set; } = default!;
    public string SaleDocumentNo { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public string Reason { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public StorePaymentMethod RefundMethod { get; set; }
    public List<StoreReturnLineResponse> Lines { get; set; } = new();
}

public class StoreShiftDocumentApprovalResponse
{
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>CartLineRemoved, CartCleared, Discount, PriceChange, CashIn, CashOut, ... as logged.</summary>
    public string Action { get; set; } = default!;
    public string? DocumentNo { get; set; }
    public string? ApprovedByName { get; set; }
    public string? Reason { get; set; }
}

public class StoreShiftDocumentResponse
{
    /// <summary>"Növbə sənədi".</summary>
    public string Name { get; set; } = default!;

    /// <summary>The document's code (NV-000123).</summary>
    public string Code { get; set; } = default!;

    public StoreShiftReportResponse Report { get; set; } = default!;

    /// <summary>How many different products and how many pieces were sold (cancelled sales left out).</summary>
    public int ProductKinds { get; set; }
    public decimal SoldQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }

    public List<StoreShiftDocumentProductResponse> Products { get; set; } = new();
    public List<StoreShiftDocumentSaleResponse> Sales { get; set; } = new();
    public List<StoreShiftDocumentReturnResponse> Returns { get; set; } = new();
    public List<StoreShiftDocumentApprovalResponse> Approvals { get; set; } = new();
}

public class StoreShiftDocumentHandler : IRequestHandler<GetStoreShiftDocumentQuery, StoreShiftDocumentResponse>
{
    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;
    private readonly Application.StoreMode.Access.StoreAccessService _access;

    public StoreShiftDocumentHandler(
        IStoreRepository repository, ICurrentUserService currentUser, IMediator mediator, Application.StoreMode.Access.StoreAccessService access)
    {
        _repository = repository;
        _currentUser = currentUser;
        _mediator = mediator;
        _access = access;
    }

    /// <summary>The code printed on the document: NV- and the shift number.</summary>
    public static string CodeOf(int shiftId) => $"NV-{shiftId:D6}";

    public async Task<StoreShiftDocumentResponse> Handle(GetStoreShiftDocumentQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var shift = await _repository.GetShiftAsync(query.ShiftId, companyId, ct)
            ?? throw new NotFoundException("Növbə tapılmadı.");
        if (await _access.AllowedBranchesAsync(ct) is { } allowedBranches && !allowedBranches.Contains(shift.RestaurantId))
            throw new NotFoundException("Növbə tapılmadı.");

        var report = await _mediator.Send(new GetStoreShiftReportQuery(shift.Id), ct);
        var sales = await _repository.GetSalesWithLinesForShiftAsync(shift.Id, ct);
        var returns = await _repository.GetReturnsWithLinesForShiftAsync(shift.Id, ct);
        var approvals = await _repository.GetApprovalLogsAsync(
            companyId, shift.CashierUserId, shift.OpenedAtUtc, shift.ClosedAtUtc ?? DateTime.UtcNow, ct);

        var userIds = new List<int>();
        userIds.AddRange(sales.Select(s => s.CancelledByUserId).OfType<int>());
        userIds.AddRange(approvals.Select(a => a.ApprovedByUserId));
        var names = await _repository.GetUserNamesAsync(userIds, ct);
        string? Name(int? id) => id is { } v && names.TryGetValue(v, out var n) ? n : null;

        var completed = sales.Where(s => s.Status == StoreSaleStatus.Completed).ToList();

        var products = completed.SelectMany(s => s.Lines)
            .GroupBy(l => l.StoreProductId)
            .ToDictionary(
                g => g.Key,
                g => new StoreShiftDocumentProductResponse
                {
                    ProductId = g.Key,
                    ProductName = g.First().ProductName,
                    Barcode = g.First().Barcode,
                    SoldQuantity = g.Sum(l => l.Quantity),
                    SoldAmount = g.Sum(l => l.LineTotal),
                });
        foreach (var line in returns.SelectMany(r => r.Lines))
        {
            if (!products.TryGetValue(line.StoreProductId, out var row))
            {
                // Returned in this shift, sold in an earlier one.
                row = new StoreShiftDocumentProductResponse { ProductId = line.StoreProductId, ProductName = line.ProductName };
                products[line.StoreProductId] = row;
            }
            row.ReturnedQuantity += line.Quantity;
            row.ReturnedAmount += line.Amount;
        }

        return new StoreShiftDocumentResponse
        {
            Name = "Növbə sənədi",
            Code = CodeOf(shift.Id),
            Report = report,
            ProductKinds = products.Values.Count(p => p.SoldQuantity > 0),
            SoldQuantity = products.Values.Sum(p => p.SoldQuantity),
            ReturnedQuantity = products.Values.Sum(p => p.ReturnedQuantity),
            Products = products.Values.OrderByDescending(p => p.SoldAmount).ThenBy(p => p.ProductName).ToList(),
            Sales = sales.Select(s => new StoreShiftDocumentSaleResponse
            {
                Id = s.Id,
                DocumentNo = s.DocumentNo,
                CreatedAtUtc = s.CreatedAtUtc,
                Status = s.Status,
                CustomerName = s.StoreCustomer?.Name,
                SubTotal = s.SubTotal,
                DiscountTotal = s.DiscountTotal,
                Total = s.Total,
                Payments = s.Payments.GroupBy(p => p.Method)
                    .Select(g => new StorePaymentTotalResponse { Method = g.Key, Amount = g.Sum(p => p.Amount) }).ToList(),
                Lines = s.Lines.Select(l => new StoreSaleLineResponse
                {
                    LineId = l.Id,
                    ProductId = l.StoreProductId,
                    ProductName = l.ProductName,
                    Barcode = l.Barcode,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    ListPrice = l.ListPrice,
                    PriceChangeReason = l.PriceChangeReason,
                    DiscountAmount = l.DiscountAmount,
                    VatPercent = l.VatPercent,
                    VatAmount = l.VatAmount,
                    LineTotal = l.LineTotal,
                }).ToList(),
                CancelledAtUtc = s.CancelledAtUtc,
                CancelledByName = Name(s.CancelledByUserId),
                CancelReason = s.CancelReason,
                IsReceiptless = s.IsReceiptless,
            }).ToList(),
            Returns = returns.Select(r => new StoreShiftDocumentReturnResponse
            {
                Id = r.Id,
                DocumentNo = r.DocumentNo,
                SaleDocumentNo = r.StoreSale?.DocumentNo ?? string.Empty,
                CreatedAtUtc = r.CreatedAtUtc,
                Reason = r.Reason,
                TotalAmount = r.TotalAmount,
                RefundAmount = r.RefundAmount,
                RefundMethod = r.RefundMethod,
                Lines = r.Lines.Select(l => new StoreReturnLineResponse
                {
                    ProductId = l.StoreProductId,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    Amount = l.Amount,
                    Condition = l.Condition,
                }).ToList(),
            }).ToList(),
            Approvals = approvals.Select(a => new StoreShiftDocumentApprovalResponse
            {
                CreatedAtUtc = a.CreatedAtUtc,
                Action = a.Action,
                DocumentNo = a.DocumentNo,
                ApprovedByName = Name(a.ApprovedByUserId),
                Reason = a.Reason,
            }).ToList(),
        };
    }
}
