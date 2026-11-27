using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Reports;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Reports;

/// <summary>
/// Settlements with counterparties, like the 1C report "Kontragentlərlə qarşılıqlı hesablaşma": for every customer (buyer) or supplier the debt at
/// the start of the period, how much it grew and shrank, the debt at the end, and the movements behind the numbers.
/// A positive debt is the party's natural direction: a customer owes the store, the store owes a supplier. A negative one is the other way round.
/// </summary>
public record GetStoreSettlementsQuery(string PartyType, DateTime From, DateTime To) : IRequest<StoreSettlementsResponse>;

public class StoreSettlementEntryResponse
{
    public DateTime EntryDate { get; set; }
    public string TypeName { get; set; } = default!;
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public string? Note { get; set; }

    /// <summary>What the debt grew by (0 when this movement shrank it).</summary>
    public decimal Increase { get; set; }

    /// <summary>What the debt shrank by (0 when this movement grew it).</summary>
    public decimal Decrease { get; set; }
}

public class StoreSettlementRowResponse
{
    public int PartyId { get; set; }
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public decimal OpeningDebt { get; set; }
    public decimal Increase { get; set; }
    public decimal Decrease { get; set; }
    public decimal ClosingDebt { get; set; }
    public List<StoreSettlementEntryResponse> Entries { get; set; } = new();
}

public class StoreSettlementsResponse
{
    /// <summary>"customers" or "suppliers".</summary>
    public string PartyType { get; set; } = default!;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal OpeningDebt { get; set; }
    public decimal Increase { get; set; }
    public decimal Decrease { get; set; }
    public decimal ClosingDebt { get; set; }
    public List<StoreSettlementRowResponse> Rows { get; set; } = new();
}

public class StoreSettlementsHandler : IRequestHandler<GetStoreSettlementsQuery, StoreSettlementsResponse>
{
    private readonly IStoreTradeRepository _trade;
    private readonly IStoreCustomerRepository _customers;
    private readonly ICurrentUserService _currentUser;

    public StoreSettlementsHandler(IStoreTradeRepository trade, IStoreCustomerRepository customers, ICurrentUserService currentUser)
    {
        _trade = trade;
        _customers = customers;
        _currentUser = currentUser;
    }

    public async Task<StoreSettlementsResponse> Handle(GetStoreSettlementsQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var kind = query.PartyType?.Trim().ToLowerInvariant();
        var partyType = kind switch
        {
            "customers" => StoreLedgerPartyType.Customer,
            "suppliers" => StoreLedgerPartyType.Supplier,
            _ => throw new BadRequestException("Tərəf növü 'customers' və ya 'suppliers' olmalıdır."),
        };
        if (query.To.Date < query.From.Date)
            throw new BadRequestException("Son tarix başlanğıcdan əvvəl ola bilməz.");

        var fromUtc = ReportPeriodResolver.StoreFromUtc(query.From);
        var toUtc = ReportPeriodResolver.StoreToUtc(query.To);

        var opening = await _trade.GetLedgerBalancesBeforeAsync(companyId, partyType, fromUtc, ct);
        var entries = (await _trade.GetLedgerForAllAsync(companyId, partyType, fromUtc, toUtc, ct))
            .GroupBy(e => e.PartyId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var names = partyType == StoreLedgerPartyType.Customer
            ? (await _customers.SearchCustomersAsync(companyId, null, true, 100_000, ct)).ToDictionary(c => c.Id, c => (c.Name, c.Phone))
            : (await _trade.GetSuppliersAsync(companyId, true, ct)).ToDictionary(s => s.Id, s => (s.Name, s.Phone));

        var rows = new List<StoreSettlementRowResponse>();
        foreach (var id in opening.Keys.Union(entries.Keys))
        {
            var open = opening.GetValueOrDefault(id);
            var moves = entries.GetValueOrDefault(id) ?? new List<Domain.Entities.StoreMode.StoreLedgerEntry>();
            var increase = moves.Where(m => m.Amount > 0).Sum(m => m.Amount);
            var decrease = -moves.Where(m => m.Amount < 0).Sum(m => m.Amount);
            var closing = open + increase - decrease;
            // A party with nothing at the start, nothing during the period and nothing at the end has no place in the report.
            if (open == 0 && moves.Count == 0)
                continue;

            names.TryGetValue(id, out var info);
            rows.Add(new StoreSettlementRowResponse
            {
                PartyId = id,
                Name = info.Name ?? $"#{id}",
                Phone = info.Phone,
                OpeningDebt = open,
                Increase = increase,
                Decrease = decrease,
                ClosingDebt = closing,
                Entries = moves.Select(m => new StoreSettlementEntryResponse
                {
                    EntryDate = m.EntryDate,
                    TypeName = m.Type.ToString(),
                    DocumentType = m.DocumentType,
                    DocumentNo = m.DocumentNo,
                    Note = m.Note,
                    Increase = m.Amount > 0 ? m.Amount : 0,
                    Decrease = m.Amount < 0 ? -m.Amount : 0,
                }).ToList(),
            });
        }

        rows = rows.OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        return new StoreSettlementsResponse
        {
            PartyType = kind!,
            From = query.From.Date,
            To = query.To.Date,
            OpeningDebt = rows.Sum(r => r.OpeningDebt),
            Increase = rows.Sum(r => r.Increase),
            Decrease = rows.Sum(r => r.Decrease),
            ClosingDebt = rows.Sum(r => r.ClosingDebt),
            Rows = rows,
        };
    }
}
