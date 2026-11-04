using Application.StoreMode;
using Application.StoreMode.Access;
using Application.StoreMode.Approvals;
using Application.StoreMode.Cash;
using Application.StoreMode.Catalog;
using Application.StoreMode.Customers;
using Application.StoreMode.Extras;
using Application.StoreMode.Fiscal;
using Application.StoreMode.Pricing;
using Application.StoreMode.Sync;
using Application.StoreMode.Reports;
using Application.StoreMode.Returns;
using Application.StoreMode.Sales;
using Application.StoreMode.Stock;
using Application.StoreMode.Trade;
using Application.StoreMode.WarehouseOps;
using Domain.Constants;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FooderaERP.Api.Controllers;

/// <summary>
/// Store (mağaza) mode: its own catalog, stock ledger, tills, shifts and sales, kept apart from the
/// restaurant side. Everything is scoped to the caller's company.
/// </summary>
[Authorize]
[ApiController]
[Route("api/store")]
[Route("api/v1/store")]
public class StoreController : ControllerBase
{
    private readonly IMediator _mediator;

    public StoreController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ---- Catalog ----------------------------------------------------------------------------

    [Authorize(Policy = AppPermissions.StoreProductView)]
    [HttpGet("categories")]
    public async Task<ActionResult<List<StoreCategoryResponse>>> GetCategories(CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreCategoriesQuery(), ct));

    [Authorize(Policy = AppPermissions.StoreProductManage)]
    [HttpPost("categories")]
    public async Task<ActionResult<StoreCategoryResponse>> CreateCategory([FromBody] SaveStoreCategoryRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreCategoryCommand(null, request), ct));

    [Authorize(Policy = AppPermissions.StoreProductManage)]
    [HttpPut("categories/{id:int}")]
    public async Task<ActionResult<StoreCategoryResponse>> UpdateCategory(int id, [FromBody] SaveStoreCategoryRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreCategoryCommand(id, request), ct));

    /// <summary>Products with paging; <c>q</c> matches name (anywhere in it), SKU or barcode. Pass <c>warehouseId</c> to get stock too.</summary>
    [Authorize(Policy = AppPermissions.StoreProductView)]
    [HttpGet("products")]
    public async Task<ActionResult<StoreProductPageResponse>> SearchProducts(
        [FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] bool? isActive,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] int? warehouseId = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new SearchStoreProductsQuery(q, categoryId, isActive, page, pageSize, warehouseId), ct));

    /// <summary>The POS scan: the active product with this barcode or SKU.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("products/by-code/{code}")]
    public async Task<ActionResult<StoreProductResponse>> FindByCode(string code, [FromQuery] int? warehouseId, CancellationToken ct)
        => Ok(await _mediator.Send(new FindStoreProductByCodeQuery(code, warehouseId), ct));

    /// <summary>Product search for the POS (name, SKU, barcode) - only needs the sale permission.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("products/pos-search")]
    public async Task<ActionResult<StoreProductPageResponse>> PosSearch(
        [FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] int? warehouseId, CancellationToken ct, [FromQuery] int limit = 40)
        => Ok(await _mediator.Send(new SearchStoreProductsQuery(q, categoryId, true, 1, Math.Clamp(limit, 1, 2000), warehouseId), ct));

    [Authorize(Policy = AppPermissions.StoreProductView)]
    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<StoreProductResponse>> GetProduct(int id, [FromQuery] int? warehouseId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreProductQuery(id, warehouseId), ct));

    [Authorize(Policy = AppPermissions.StoreProductManage)]
    [HttpPost("products")]
    public async Task<ActionResult<StoreProductResponse>> CreateProduct([FromBody] SaveStoreProductRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreProductCommand(null, request), ct));

    [Authorize(Policy = AppPermissions.StoreProductManage)]
    [HttpPut("products/{id:int}")]
    public async Task<ActionResult<StoreProductResponse>> UpdateProduct(int id, [FromBody] SaveStoreProductRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreProductCommand(id, request), ct));

    /// <summary>All products as a CSV file (opens in Excel).</summary>
    [Authorize(Policy = AppPermissions.StoreProductView)]
    [HttpGet("products/export")]
    public async Task<IActionResult> ExportProducts(CancellationToken ct)
    {
        var csv = await _mediator.Send(new ExportStoreProductsQuery(), ct);
        return File(new System.Text.UTF8Encoding(true).GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv; charset=utf-8", "products.csv");
    }

    /// <summary>Creates or updates products from an uploaded CSV file (same columns as the export).</summary>
    [Authorize(Policy = AppPermissions.StoreProductManage)]
    [HttpPost("products/import")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<StoreProductImportResult>> ImportProducts(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Fayl seçilməyib." });
        using var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8);
        return Ok(await _mediator.Send(new ImportStoreProductsCommand(await reader.ReadToEndAsync(ct)), ct));
    }

    /// <summary>Bonus on or off for the chosen products (or for everything a search / category shows).</summary>
    [Authorize(Policy = AppPermissions.StoreProductManage)]
    [HttpPost("products/bonus")]
    public async Task<ActionResult<SetStoreProductsBonusResponse>> SetProductsBonus([FromBody] SetStoreProductsBonusRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SetStoreProductsBonusCommand(request), ct));

    // ---- Stock ------------------------------------------------------------------------------

    /// <summary>Current stock per warehouse and product; <c>lowOnly</c> keeps those at or below their minimum.</summary>
    [Authorize(Policy = AppPermissions.StoreStockView)]
    [HttpGet("stock")]
    public async Task<ActionResult<List<StoreStockBalanceResponse>>> GetStock(
        [FromQuery] int? warehouseId, [FromQuery] string? q, [FromQuery] bool lowOnly = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStoreStockQuery(warehouseId, q, lowOnly), ct));

    /// <summary>The stock ledger: every change with the balance before and after, who and when.</summary>
    [Authorize(Policy = AppPermissions.StoreStockView)]
    [HttpGet("stock/movements")]
    public async Task<ActionResult<List<StoreStockMovementResponse>>> GetMovements(
        [FromQuery] int? warehouseId, [FromQuery] int? productId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int take = 200, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStoreMovementsQuery(warehouseId, productId, from, to, take), ct));

    /// <summary>Raises or lowers stock by hand (also waste and internal use) with a required reason.</summary>
    [Authorize(Policy = AppPermissions.StoreStockAdjust)]
    [HttpPost("stock/adjust")]
    public async Task<ActionResult<StoreStockBalanceResponse>> Adjust([FromBody] AdjustStoreStockRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new AdjustStoreStockCommand(request), ct));

    [Authorize(Policy = AppPermissions.StoreStockView)]
    [HttpGet("settings")]
    public async Task<ActionResult<StoreSettingsResponse>> GetSettings(CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreSettingsQuery(), ct));

    [Authorize(Policy = AppPermissions.StoreSettingsManage)]
    [HttpPut("settings")]
    public async Task<ActionResult<StoreSettingsResponse>> UpdateSettings([FromBody] StoreSettingsResponse request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateStoreSettingsCommand(request), ct));

    // ---- Tills and shifts -------------------------------------------------------------------

    [Authorize]
    [HttpGet("registers")]
    public async Task<ActionResult<List<StoreCashRegisterResponse>>> GetRegisters([FromQuery] int? restaurantId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreRegistersQuery(restaurantId), ct));

    [Authorize(Policy = AppPermissions.StoreRegisterManage)]
    [HttpPost("registers")]
    public async Task<ActionResult<StoreCashRegisterResponse>> CreateRegister([FromBody] SaveStoreCashRegisterRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreRegisterCommand(null, request), ct));

    [Authorize(Policy = AppPermissions.StoreRegisterManage)]
    [HttpPut("registers/{id:int}")]
    public async Task<ActionResult<StoreCashRegisterResponse>> UpdateRegister(int id, [FromBody] SaveStoreCashRegisterRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreRegisterCommand(id, request), ct));

    /// <summary>The current user's open shift; 204 when there is none.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("shifts/mine")]
    public async Task<ActionResult<StoreShiftResponse>> GetMyShift(CancellationToken ct)
    {
        var shift = await _mediator.Send(new GetMyStoreShiftQuery(), ct);
        return shift is null ? NoContent() : Ok(shift);
    }

    [Authorize(Policy = AppPermissions.StoreCashOpen)]
    [HttpPost("shifts/open")]
    public async Task<ActionResult<StoreShiftResponse>> OpenShift([FromBody] OpenStoreShiftRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new OpenStoreShiftCommand(request), ct));

    [Authorize(Policy = AppPermissions.StoreReportView)]
    [HttpGet("shifts")]
    public async Task<ActionResult<List<StoreShiftResponse>>> GetShifts(
        [FromQuery] int? registerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreShiftsQuery(registerId, from, to), ct));

    /// <summary>X-Report of an open shift (does not close it) or Z-Report of a closed one.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("shifts/{id:int}/report")]
    public async Task<ActionResult<StoreShiftReportResponse>> GetShiftReport(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreShiftReportQuery(id), ct));

    /// <summary>Z-Report: counts the till, closes the shift and returns the final report.</summary>
    [Authorize(Policy = AppPermissions.StoreCashClose)]
    [HttpPost("shifts/{id:int}/close")]
    public async Task<ActionResult<StoreShiftReportResponse>> CloseShift(int id, [FromBody] CloseStoreShiftRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CloseStoreShiftCommand(id, request), ct));

    /// <summary>Opens a closed shift again; needs its own permission and a reason (audited).</summary>
    [Authorize(Policy = AppPermissions.StoreCashReopen)]
    [HttpPost("shifts/{id:int}/reopen")]
    public async Task<ActionResult<StoreShiftResponse>> ReopenShift(int id, [FromBody] ReopenStoreShiftRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new ReopenStoreShiftCommand(id, request), ct));

    /// <summary>Cash put into or taken out of the till outside a sale.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("shifts/{id:int}/cash-movements")]
    public async Task<ActionResult<StoreShiftReportResponse>> AddCashMovement(int id, [FromBody] StoreCashMovementRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new AddStoreCashMovementCommand(id, request), ct));

    /// <summary>What needs attention right now: low stock, shifts left open, overdue debts, stuck transfers (only what the caller may see).</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("alerts")]
    public async Task<ActionResult<List<StoreAlertResponse>>> GetAlerts(CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreAlertsQuery(), ct));

    /// <summary>Sends a sale to the fiscal device again after it failed.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleApprove)]
    [HttpPost("sales/{id:int}/fiscal-retry")]
    public async Task<ActionResult<StoreSaleResponse>> RetryFiscal(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new RetryStoreFiscalCommand(id), ct));

    // ---- Cart approvals ---------------------------------------------------------------------

    /// <summary>Whether the cashier may take a product out of the cart / clear it; without the permission a manager's credentials are needed.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("approvals/check")]
    public async Task<ActionResult<CheckStoreApprovalResponse>> CheckApproval([FromBody] CheckStoreApprovalRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CheckStoreApprovalCommand(request), ct));

    // ---- Payment reversal -------------------------------------------------------------------

    /// <summary>Cancels a customer's debt payment (reason required; its shift must still be open).</summary>
    [Authorize(Policy = AppPermissions.StoreCustomerPay)]
    [HttpPost("customer-payments/{id:int}/reverse")]
    public async Task<ActionResult<StoreCustomerStatementResponse>> ReverseCustomerPayment(int id, [FromBody] ReverseStorePaymentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new ReverseStoreCustomerPaymentCommand(id, request), ct));

    /// <summary>Cancels a payment made to a supplier by a counter entry; the purchases it paid become unpaid again.</summary>
    [Authorize(Policy = AppPermissions.StoreSupplierPay)]
    [HttpPost("supplier-payments/{id:int}/reverse")]
    public async Task<ActionResult<StoreSupplierPaymentResponse>> ReverseSupplierPayment(int id, [FromBody] ReverseStorePaymentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new ReverseStoreSupplierPaymentCommand(id, request), ct));

    // ---- Branch prices ----------------------------------------------------------------------

    /// <summary>The product's own price per branch (a branch not listed uses the normal sale price).</summary>
    [Authorize(Policy = AppPermissions.StoreProductView)]
    [HttpGet("products/{id:int}/branch-prices")]
    public async Task<ActionResult<List<StoreBranchPriceItem>>> GetBranchPrices(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreBranchPricesQuery(id), ct));

    [Authorize(Policy = AppPermissions.StoreProductManage)]
    [HttpPut("products/{id:int}/branch-prices")]
    public async Task<ActionResult<List<StoreBranchPriceItem>>> SaveBranchPrices(int id, [FromBody] SaveStoreBranchPricesRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreBranchPricesCommand(id, request), ct));

    // ---- User access ------------------------------------------------------------------------

    /// <summary>The branches a user is limited to (empty = all).</summary>
    [Authorize(Policy = AppPermissions.StoreSettingsManage)]
    [HttpGet("user-access/{userId:int}")]
    public async Task<ActionResult<StoreUserAccessResponse>> GetUserAccess(int userId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreUserAccessQuery(userId), ct));

    [Authorize(Policy = AppPermissions.StoreSettingsManage)]
    [HttpPut("user-access/{userId:int}")]
    public async Task<ActionResult<StoreUserAccessResponse>> SaveUserAccess(int userId, [FromBody] SaveStoreUserAccessRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreUserAccessCommand(userId, request), ct));

    // ---- Offline sync -----------------------------------------------------------------------

    /// <summary>A till that was offline sends its waiting operations; each is handled once (by OperationId) and answered separately.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("sync/push")]
    public async Task<ActionResult<List<StoreSyncOperationResult>>> SyncPush([FromBody] StoreSyncPushRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new PushStoreSyncCommand(request), ct));

    /// <summary>What a till needs to keep selling offline: active products with prices, barcodes and the warehouse's stock, plus the rules.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("sync/pull")]
    public async Task<ActionResult<StoreSyncPullResponse>> SyncPull([FromQuery] int? warehouseId, CancellationToken ct)
        => Ok(await _mediator.Send(new PullStoreSyncQuery(warehouseId), ct));

    /// <summary>Sync log: what devices sent and how each ended, including the reason of a refused one.</summary>
    [Authorize(Policy = AppPermissions.StoreReportView)]
    [HttpGet("sync/operations")]
    public async Task<ActionResult<List<StoreSyncOperationResult>>> SyncOperations([FromQuery] StoreSyncStatus? status, [FromQuery] string? deviceId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreSyncOperationsQuery(status, deviceId), ct));

    // ---- Reports ----------------------------------------------------------------------------

    /// <summary>Sales, discounts, returns, cost and profit for a period, with breakdowns by day, product, category, cashier, till and payment method.</summary>
    [Authorize(Policy = AppPermissions.StoreReportFinance)]
    [HttpGet("reports/sales")]
    public async Task<ActionResult<StoreSalesReportResponse>> GetSalesReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? restaurantId, [FromQuery] int? warehouseId,
        [FromQuery] int? cashierUserId, [FromQuery] int? categoryId, [FromQuery] int? productId, [FromQuery] bool includeUnsold = false,
        CancellationToken ct = default)
    {
        var day = DateTime.UtcNow.Date;
        return Ok(await _mediator.Send(
            new GetStoreSalesReportQuery(from ?? day, to ?? from ?? day, restaurantId, warehouseId, cashierUserId, categoryId, productId, includeUnsold), ct));
    }

    /// <summary>The grouped sales report: chosen groupings (day, customer, register, supplier, product, document) with totals and profit, filtered by lists.</summary>
    [Authorize(Policy = AppPermissions.StoreReportFinance)]
    [HttpPost("reports/sales-matrix")]
    public async Task<ActionResult<StoreSalesMatrixResponse>> GetSalesMatrix([FromBody] StoreSalesMatrixRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreSalesMatrixQuery(request), ct));

    /// <summary>Cash of every till: opening balance, money in, money out and closing balance for the period.</summary>
    [Authorize(Policy = AppPermissions.StoreReportView)]
    [HttpGet("reports/cash-summary")]
    public async Task<ActionResult<StoreCashSummaryResponse>> GetCashSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var day = DateTime.UtcNow.Date;
        return Ok(await _mediator.Send(new GetStoreCashSummaryQuery(from ?? day, to ?? from ?? day), ct));
    }

    /// <summary>Rating of the sold products by quantity (most sold first, or least sold first) with code, barcode, price, quantity and amount.</summary>
    [Authorize(Policy = AppPermissions.StoreReportView)]
    [HttpGet("reports/product-rating")]
    public async Task<ActionResult<StoreProductRatingResponse>> GetProductRating(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] bool least = false, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var day = DateTime.UtcNow.Date;
        return Ok(await _mediator.Send(new GetStoreProductRatingQuery(from ?? day, to ?? from ?? day, least, take), ct));
    }

    /// <summary>Stock of every warehouse: per product the opening balance, what came in and went out, and the closing balance, in quantity and value.</summary>
    [Authorize(Policy = AppPermissions.StoreReportView)]
    [HttpGet("reports/stock-summary")]
    public async Task<ActionResult<StoreStockSummaryResponse>> GetStockSummary(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? warehouseId, [FromQuery] int? categoryId, CancellationToken ct)
    {
        var day = DateTime.UtcNow.Date;
        return Ok(await _mediator.Send(new GetStoreStockSummaryQuery(from ?? day.AddDays(-29), to ?? day, warehouseId, categoryId), ct));
    }

    /// <summary>Stock and its value, products below minimum, and the period's movements by kind.</summary>
    [Authorize(Policy = AppPermissions.StoreReportView)]
    [HttpGet("reports/stock")]
    public async Task<ActionResult<StoreStockReportResponse>> GetStockReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? warehouseId, [FromQuery] int? categoryId, CancellationToken ct)
    {
        var day = DateTime.UtcNow.Date;
        return Ok(await _mediator.Send(new GetStoreStockReportQuery(from ?? day.AddDays(-29), to ?? day, warehouseId, categoryId), ct));
    }

    /// <summary>Cash in/out outside sales and the counted-versus-expected difference of every shift.</summary>
    [Authorize(Policy = AppPermissions.StoreReportView)]
    [HttpGet("reports/cash")]
    public async Task<ActionResult<StoreCashReportResponse>> GetCashReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? restaurantId, CancellationToken ct)
    {
        var day = DateTime.UtcNow.Date;
        return Ok(await _mediator.Send(new GetStoreCashReportQuery(from ?? day, to ?? day, restaurantId), ct));
    }

    /// <summary>Who owes the store, with aging of the unpaid credit.</summary>
    [Authorize(Policy = AppPermissions.StoreReportFinance)]
    [HttpGet("reports/customer-debts")]
    public async Task<ActionResult<StoreCustomerDebtReportResponse>> GetCustomerDebtReport(CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreCustomerDebtReportQuery(), ct));

    /// <summary>Bonus earned, spent, taken back and corrected per customer.</summary>
    [Authorize(Policy = AppPermissions.StoreReportFinance)]
    [HttpGet("reports/bonus")]
    public async Task<ActionResult<StoreBonusReportResponse>> GetBonusReport([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var day = DateTime.UtcNow.Date;
        return Ok(await _mediator.Send(new GetStoreBonusReportQuery(from ?? day.AddDays(-29), to ?? day), ct));
    }

    // ---- Customers, bonus and credit --------------------------------------------------------

    /// <summary>Customers by name, code or phone. The till needs only the sale permission.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("customers")]
    public async Task<ActionResult<List<StoreCustomerResponse>>> SearchCustomers(
        [FromQuery] string? q, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new SearchStoreCustomersQuery(q, includeInactive), ct));

    /// <summary>The till's lookup: by customer code or phone number.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("customers/find")]
    public async Task<ActionResult<StoreCustomerResponse>> FindCustomer([FromQuery] string term, CancellationToken ct)
        => Ok(await _mediator.Send(new FindStoreCustomerQuery(term), ct));

    /// <summary>A customer card can be made at the till.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("customers")]
    public async Task<ActionResult<StoreCustomerResponse>> CreateCustomer([FromBody] SaveStoreCustomerRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreCustomerCommand(null, request), ct));

    [Authorize(Policy = AppPermissions.StoreCustomerManage)]
    [HttpPut("customers/{id:int}")]
    public async Task<ActionResult<StoreCustomerResponse>> UpdateCustomer(int id, [FromBody] SaveStoreCustomerRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreCustomerCommand(id, request), ct));

    /// <summary>A customer's debt ledger, bonus ledger and payments.</summary>
    [Authorize(Policy = AppPermissions.StoreCustomerView)]
    [HttpGet("customers/{id:int}/statement")]
    public async Task<ActionResult<StoreCustomerStatementResponse>> GetCustomerStatement(
        int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreCustomerStatementQuery(id, from, to), ct));

    /// <summary>Takes a payment towards a customer's debt at the till.</summary>
    [Authorize(Policy = AppPermissions.StoreCustomerPay)]
    [HttpPost("customer-payments")]
    public async Task<ActionResult<StoreCustomerStatementResponse>> PayCustomerDebt([FromBody] CreateStoreCustomerPaymentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateStoreCustomerPaymentCommand(request), ct));

    /// <summary>Corrects a customer's bonus by hand, with a reason; it is written to the bonus ledger.</summary>
    [Authorize(Policy = AppPermissions.StoreBonusAdjust)]
    [HttpPost("bonus/adjustments")]
    public async Task<ActionResult<StoreCustomerResponse>> AdjustBonus([FromBody] AdjustStoreBonusRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new AdjustStoreBonusCommand(request), ct));

    // ---- Suppliers, purchases and the ledger ------------------------------------------------

    [Authorize(Policy = AppPermissions.StoreSupplierView)]
    [HttpGet("suppliers")]
    public async Task<ActionResult<List<StoreSupplierResponse>>> GetSuppliers([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetStoreSuppliersQuery(includeInactive), ct));

    [Authorize(Policy = AppPermissions.StoreSupplierManage)]
    [HttpPost("suppliers")]
    public async Task<ActionResult<StoreSupplierResponse>> CreateSupplier([FromBody] SaveStoreSupplierRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreSupplierCommand(null, request), ct));

    [Authorize(Policy = AppPermissions.StoreSupplierManage)]
    [HttpPut("suppliers/{id:int}")]
    public async Task<ActionResult<StoreSupplierResponse>> UpdateSupplier(int id, [FromBody] SaveStoreSupplierRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreSupplierCommand(id, request), ct));

    /// <summary>A supplier's account for a period: opening balance, every ledger movement with the running balance, closing balance, payments.</summary>
    [Authorize(Policy = AppPermissions.StoreSupplierView)]
    [HttpGet("suppliers/{id:int}/statement")]
    public async Task<ActionResult<StoreSupplierStatementResponse>> GetSupplierStatement(
        int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreSupplierStatementQuery(id, from, to), ct));

    /// <summary>Pays a supplier: against chosen purchases or, left open, the oldest unpaid ones first; the excess stays as an advance.</summary>
    [Authorize(Policy = AppPermissions.StoreSupplierPay)]
    [HttpPost("supplier-payments")]
    public async Task<ActionResult<StoreSupplierPaymentResponse>> PaySupplier([FromBody] CreateStoreSupplierPaymentRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateStoreSupplierPaymentCommand(request), ct));

    /// <summary>Who the store owes, with aging (not due, 1-30, 31-60, 61-90, 90+ days overdue) and the unpaid purchases.</summary>
    [Authorize(Policy = AppPermissions.StoreSupplierView)]
    [HttpGet("suppliers/debts")]
    public async Task<ActionResult<StoreSupplierDebtReportResponse>> GetSupplierDebts(CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreSupplierDebtReportQuery(), ct));

    [Authorize(Policy = AppPermissions.StorePurchaseView)]
    [HttpGet("purchases")]
    public async Task<ActionResult<StorePurchasePageResponse>> SearchPurchases(
        [FromQuery] int? supplierId, [FromQuery] StorePurchaseStatus? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _mediator.Send(new SearchStorePurchasesQuery(supplierId, status, from, to, q, page, pageSize), ct));

    [Authorize(Policy = AppPermissions.StorePurchaseView)]
    [HttpGet("purchases/{id:int}")]
    public async Task<ActionResult<StorePurchaseResponse>> GetPurchase(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStorePurchaseQuery(id), ct));

    /// <summary>Creates a draft purchase; a draft changes neither stock nor the supplier's balance.</summary>
    [Authorize(Policy = AppPermissions.StorePurchaseManage)]
    [HttpPost("purchases")]
    public async Task<ActionResult<StorePurchaseResponse>> CreatePurchase([FromBody] SaveStorePurchaseRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStorePurchaseCommand(null, request), ct));

    [Authorize(Policy = AppPermissions.StorePurchaseManage)]
    [HttpPut("purchases/{id:int}")]
    public async Task<ActionResult<StorePurchaseResponse>> UpdatePurchase(int id, [FromBody] SaveStorePurchaseRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStorePurchaseCommand(id, request), ct));

    /// <summary>Receives the goods: stock rises, the average cost updates and the supplier's balance grows. An immediate payment may be sent.</summary>
    [Authorize(Policy = AppPermissions.StorePurchaseConfirm)]
    [HttpPost("purchases/{id:int}/confirm")]
    public async Task<ActionResult<StorePurchaseResponse>> ConfirmPurchase(int id, [FromBody] ConfirmStorePurchaseRequest? request, CancellationToken ct)
        => Ok(await _mediator.Send(new ConfirmStorePurchaseCommand(id, request ?? new ConfirmStorePurchaseRequest()), ct));

    [Authorize(Policy = AppPermissions.StorePurchaseConfirm)]
    [HttpPost("purchases/{id:int}/cancel")]
    public async Task<ActionResult<StorePurchaseResponse>> CancelPurchase(int id, [FromBody] CancelStorePurchaseRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelStorePurchaseCommand(id, request), ct));

    /// <summary>Hands goods back to the supplier: stock drops and the supplier's balance shrinks.</summary>
    [Authorize(Policy = AppPermissions.StorePurchaseReturn)]
    [HttpPost("purchases/{id:int}/return")]
    public async Task<ActionResult<StorePurchaseResponse>> ReturnPurchase(int id, [FromBody] ReturnStorePurchaseRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new ReturnStorePurchaseCommand(id, request), ct));

    [Authorize(Policy = AppPermissions.StoreProductView)]
    [HttpGet("products/{id:int}/price-history")]
    public async Task<ActionResult<List<StorePriceHistoryResponse>>> GetPriceHistory(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStorePriceHistoryQuery(id), ct));

    // ---- Inventory (stock count) and transfers ----------------------------------------------

    [Authorize(Policy = AppPermissions.StoreInventoryView)]
    [HttpGet("inventories")]
    public async Task<ActionResult<List<StoreInventoryResponse>>> GetInventories(
        [FromQuery] int? warehouseId, [FromQuery] StoreInventoryStatus? status, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreInventoriesQuery(warehouseId, status), ct));

    [Authorize(Policy = AppPermissions.StoreInventoryView)]
    [HttpGet("inventories/{id:int}")]
    public async Task<ActionResult<StoreInventoryResponse>> GetInventory(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreInventoryQuery(id), ct));

    /// <summary>Opens a count of a warehouse (or one category): what the system expects is frozen, stock is not touched.</summary>
    [Authorize(Policy = AppPermissions.StoreInventoryManage)]
    [HttpPost("inventories")]
    public async Task<ActionResult<StoreInventoryResponse>> StartInventory([FromBody] StartStoreInventoryRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new StartStoreInventoryCommand(request), ct));

    [Authorize(Policy = AppPermissions.StoreInventoryManage)]
    [HttpPut("inventories/{id:int}/counts")]
    public async Task<ActionResult<StoreInventoryResponse>> SaveInventoryCounts(int id, [FromBody] SaveStoreInventoryCountsRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreInventoryCountsCommand(id, request), ct));

    /// <summary>Confirms the count: the differences become stock adjustments.</summary>
    [Authorize(Policy = AppPermissions.StoreInventoryConfirm)]
    [HttpPost("inventories/{id:int}/confirm")]
    public async Task<ActionResult<StoreInventoryResponse>> ConfirmInventory(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new ConfirmStoreInventoryCommand(id), ct));

    [Authorize(Policy = AppPermissions.StoreInventoryManage)]
    [HttpPost("inventories/{id:int}/cancel")]
    public async Task<ActionResult<StoreInventoryResponse>> CancelInventory(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelStoreInventoryCommand(id), ct));

    [Authorize(Policy = AppPermissions.StoreTransferView)]
    [HttpGet("transfers")]
    public async Task<ActionResult<List<StoreTransferResponse>>> GetTransfers(
        [FromQuery] StoreTransferStatus? status, [FromQuery] int? warehouseId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreTransfersQuery(status, warehouseId, from, to), ct));

    [Authorize(Policy = AppPermissions.StoreTransferView)]
    [HttpGet("transfers/{id:int}")]
    public async Task<ActionResult<StoreTransferResponse>> GetTransfer(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreTransferQuery(id), ct));

    [Authorize(Policy = AppPermissions.StoreTransferManage)]
    [HttpPost("transfers")]
    public async Task<ActionResult<StoreTransferResponse>> CreateTransfer([FromBody] SaveStoreTransferRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreTransferCommand(null, request), ct));

    [Authorize(Policy = AppPermissions.StoreTransferManage)]
    [HttpPut("transfers/{id:int}")]
    public async Task<ActionResult<StoreTransferResponse>> UpdateTransfer(int id, [FromBody] SaveStoreTransferRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreTransferCommand(id, request), ct));

    [Authorize(Policy = AppPermissions.StoreTransferManage)]
    [HttpPost("transfers/{id:int}/submit")]
    public async Task<ActionResult<StoreTransferResponse>> SubmitTransfer(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new SubmitStoreTransferCommand(id), ct));

    /// <summary>Ships the transfer: the goods leave the source warehouse and are in transit until received.</summary>
    [Authorize(Policy = AppPermissions.StoreTransferShip)]
    [HttpPost("transfers/{id:int}/ship")]
    public async Task<ActionResult<StoreTransferResponse>> ShipTransfer(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new ShipStoreTransferCommand(id), ct));

    /// <summary>Receives what really arrived (possibly in several steps); <c>closeWithShortage</c> closes it and notes what is missing.</summary>
    [Authorize(Policy = AppPermissions.StoreTransferReceive)]
    [HttpPost("transfers/{id:int}/receive")]
    public async Task<ActionResult<StoreTransferResponse>> ReceiveTransfer(int id, [FromBody] ReceiveStoreTransferRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new ReceiveStoreTransferCommand(id, request), ct));

    [Authorize(Policy = AppPermissions.StoreTransferManage)]
    [HttpPost("transfers/{id:int}/cancel")]
    public async Task<ActionResult<StoreTransferResponse>> CancelTransfer(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelStoreTransferCommand(id), ct));

    // ---- Quick slots and held sales ---------------------------------------------------------

    /// <summary>The company's 100 quick-sale buttons (shared by every till; empty slots included). The till only decides the branch price shown.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("registers/{registerId:int}/quick-slots")]
    public async Task<ActionResult<List<StoreQuickSlotResponse>>> GetQuickSlots(int registerId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreQuickSlotsQuery(registerId), ct));

    /// <summary>Puts a product on a slot, or empties it when <c>productId</c> is null.</summary>
    [Authorize(Policy = AppPermissions.StoreRegisterManage)]
    [HttpPut("registers/{registerId:int}/quick-slots")]
    public async Task<ActionResult<List<StoreQuickSlotResponse>>> SaveQuickSlot(
        int registerId, [FromBody] SaveStoreQuickSlotRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SaveStoreQuickSlotCommand(registerId, request), ct));

    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("registers/{registerId:int}/held-sales")]
    public async Task<ActionResult<List<StoreHeldSaleResponse>>> GetHeldSales(int registerId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreHeldSalesQuery(registerId), ct));

    /// <summary>Sets a basket aside (hold). Stock and money are not touched.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("held-sales")]
    public async Task<ActionResult<StoreHeldSaleResponse>> HoldSale([FromBody] HoldStoreSaleRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new HoldStoreSaleCommand(request), ct));

    /// <summary>Takes a held basket back: returns its lines and removes it from the held list.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("held-sales/{id:int}/resume")]
    public async Task<ActionResult<StoreHeldSaleResponse>> ResumeHeldSale(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new ResumeStoreHeldSaleCommand(id), ct));

    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpDelete("held-sales/{id:int}")]
    public async Task<IActionResult> DiscardHeldSale(int id, CancellationToken ct)
    {
        await _mediator.Send(new DiscardStoreHeldSaleCommand(id), ct);
        return NoContent();
    }

    // ---- Serial / IMEI ----------------------------------------------------------------------

    /// <summary>Serial / IMEI units; the POS asks for one product's units that are in stock in its warehouse.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("serials")]
    public async Task<ActionResult<List<StoreSerialUnitResponse>>> SearchSerials(
        [FromQuery] string? q, [FromQuery] int? productId, [FromQuery] int? warehouseId, [FromQuery] StoreSerialStatus? status, CancellationToken ct)
        => Ok(await _mediator.Send(new SearchStoreSerialsQuery(q, productId, warehouseId, status), ct));

    /// <summary>One unit's whole history: received, sold, returned, written off.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("serials/{id:int}")]
    public async Task<ActionResult<StoreSerialUnitResponse>> GetSerial(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreSerialHistoryQuery(id), ct));

    // ---- Returns and exchange ---------------------------------------------------------------

    /// <summary>Finds a receipt by its number and shows what can still be returned from it.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpGet("returns/find-sale")]
    public async Task<ActionResult<StoreSaleForReturnResponse>> FindSaleForReturn([FromQuery] string documentNo, CancellationToken ct)
        => Ok(await _mediator.Send(new FindStoreSaleForReturnQuery(documentNo), ct));

    /// <summary>Takes goods back against a sale; a cashier without the return permission sends a manager's credentials in <c>approval</c>.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("returns")]
    public async Task<ActionResult<StoreReturnResponse>> CreateReturn([FromBody] CreateStoreReturnRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateStoreReturnCommand(request), ct));

    /// <summary>Exchange: goods come back and new goods are sold in one step; the price difference is paid or refunded.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("returns/exchange")]
    public async Task<ActionResult<StoreExchangeResponse>> CreateExchange([FromBody] CreateStoreExchangeRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateStoreExchangeCommand(request), ct));

    [Authorize(Policy = AppPermissions.StoreSaleView)]
    [HttpGet("returns")]
    public async Task<ActionResult<List<StoreReturnResponse>>> SearchReturns([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _mediator.Send(new SearchStoreReturnsQuery(from, to), ct));

    [Authorize(Policy = AppPermissions.StoreSaleView)]
    [HttpGet("returns/{id:int}")]
    public async Task<ActionResult<StoreReturnResponse>> GetReturn(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreReturnQuery(id), ct));

    // ---- Sales ------------------------------------------------------------------------------

    /// <summary>Completes a sale in one step: stock drops and payments are recorded together. Resending the same <c>operationId</c> returns the first sale.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("sales")]
    public async Task<ActionResult<StoreSaleResponse>> CreateSale([FromBody] CreateStoreSaleRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateStoreSaleCommand(request), ct));

    [Authorize(Policy = AppPermissions.StoreSaleView)]
    [HttpGet("sales")]
    public async Task<ActionResult<StoreSalePageResponse>> SearchSales(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? shiftId, [FromQuery] StoreSaleStatus? status,
        [FromQuery] string? documentNo, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _mediator.Send(new SearchStoreSalesQuery(from, to, shiftId, status, documentNo, page, pageSize), ct));

    [Authorize(Policy = AppPermissions.StoreSaleView)]
    [HttpGet("sales/{id:int}")]
    public async Task<ActionResult<StoreSaleResponse>> GetSale(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStoreSaleQuery(id), ct));

    /// <summary>Cancels a sale of a still-open shift: stock goes back, the sale stays on record with the reason. A cashier without the cancel permission sends a manager's credentials in <c>approval</c>.</summary>
    [Authorize(Policy = AppPermissions.StoreSaleCreate)]
    [HttpPost("sales/{id:int}/cancel")]
    public async Task<ActionResult<StoreSaleResponse>> CancelSale(int id, [FromBody] CancelStoreSaleRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelStoreSaleCommand(id, request), ct));
}
