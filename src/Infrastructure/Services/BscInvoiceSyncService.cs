using Domain.Entities.BscInvoice;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Services;

public class BscInvoiceSyncService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BscInvoiceSyncService> _logger;

    public BscInvoiceSyncService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<BscInvoiceSyncService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(int inserted, int skipped)> SyncAsync(DateTime? targetDate = null, CancellationToken cancellationToken = default)
    {
        var date = targetDate ?? DateTime.Today.AddDays(-1);

        _logger.LogInformation("BSC sync started for date {Date}", date.ToString("yyyy-MM-dd"));

        var connStr = _configuration.GetConnectionString("BscConnection")
            ?? "Host=192.168.7.155;Port=5432;Database=uyumtest;Username=uyum;Password=12345;";

        var bscInvoices = await FetchFromBscAsync(connStr, date, cancellationToken);

        if (bscInvoices.Count == 0)
        {
            _logger.LogInformation("No BSC invoices found for {Date}", date.ToString("yyyy-MM-dd"));
            return (0, 0);
        }

        var existingIds = await _context.BscInvoiceMs
            .Where(m => m.DocDate.Date == date.Date)
            .Select(m => m.BscInvoiceMId)
            .ToListAsync(cancellationToken);

        var toInsert = bscInvoices.Where(m => !existingIds.Contains(m.BscInvoiceMId)).ToList();
        var skipped = bscInvoices.Count - toInsert.Count;

        if (toInsert.Count > 0)
        {
            await _context.BscInvoiceMs.AddRangeAsync(toInsert, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("BSC sync done. Inserted: {Inserted}, Skipped: {Skipped}", toInsert.Count, skipped);

        return (toInsert.Count, skipped);
    }

    private static async Task<List<BscInvoiceM>> FetchFromBscAsync(
        string connStr, DateTime date, CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, BscInvoiceM>();

        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync(cancellationToken);

        var mSql = @"
            SELECT invoice_m_id, doc_no, doc_date, entity_id, branch_id, co_id,
                   amt, amt_vat, purchase_sales, create_date
            FROM uyumsoft.psmt_invoice_m
            WHERE doc_date::date = @date";

        await using (var cmd = new NpgsqlCommand(mSql, conn))
        {
            cmd.Parameters.AddWithValue("date", date.Date);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var m = new BscInvoiceM
                {
                    BscInvoiceMId = reader.GetInt32(0),
                    DocNo         = reader.IsDBNull(1) ? null : reader.GetString(1),
                    DocDate       = reader.GetDateTime(2),
                    EntityId      = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    BranchId      = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    CoId          = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    Amt           = reader.GetDecimal(6),
                    AmtVat        = reader.GetDecimal(7),
                    PurchaseSales = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    BscCreateDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                };
                result[m.BscInvoiceMId] = m;
            }
        }

        if (result.Count == 0) return new List<BscInvoiceM>();

        var ids = string.Join(",", result.Keys);
        var dSql = $@"
            SELECT invoice_d_id, invoice_m_id, line_no, item_id, qty, unit_price,
                   amt, amt_vat, vat_rate, branch_id, co_id, doc_date, create_date
            FROM uyumsoft.psmt_invoice_d
            WHERE invoice_m_id IN ({ids})";

        await using (var cmd = new NpgsqlCommand(dSql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var mId = reader.GetInt32(1);
                if (!result.TryGetValue(mId, out var parent)) continue;

                parent.Lines.Add(new BscInvoiceD
                {
                    BscInvoiceDId = reader.GetInt32(0),
                    BscInvoiceMId = mId,
                    LineNo        = reader.GetInt32(2),
                    ItemId        = reader.IsDBNull(3)  ? null : reader.GetInt32(3),
                    Qty           = reader.GetDecimal(4),
                    UnitPrice     = reader.GetDecimal(5),
                    Amt           = reader.GetDecimal(6),
                    AmtVat        = reader.GetDecimal(7),
                    VatRate       = reader.GetDecimal(8),
                    BranchId      = reader.IsDBNull(9)  ? null : reader.GetInt32(9),
                    CoId          = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    DocDate       = reader.GetDateTime(11),
                    BscCreateDate = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                });
            }
        }

        return result.Values.ToList();
    }
}
