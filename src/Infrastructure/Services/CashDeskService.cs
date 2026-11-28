using Application.Analytics;
using Application.BackOffice;
using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Reports;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class CashDeskService : ICashDeskService
{
    private readonly AppDbContext _db;

    public CashDeskService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CashDeskSummary> GetSummaryAsync(int companyId, int restaurantId, CancellationToken ct)
    {
        if (!await _db.Restaurants.AnyAsync(r => r.Id == restaurantId && r.CompanyId == companyId, ct))
            throw new NotFoundException("Filial tapılmadı.");

        var settings = await _db.CompanySettings.AsNoTracking().FirstOrDefaultAsync(s => s.CompanyId == companyId, ct);
        var dayStart = ReportPeriodResolver.DayStartFor(settings);
        var dayFrom = BusinessTime.ToUtc((BusinessTime.Now - dayStart).Date + dayStart);

        var today = await TotalsAsync(companyId, restaurantId, dayFrom, ct);
        var shift = await _db.Shifts.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.RestaurantId == restaurantId && s.IsOpen)
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(ct);
        decimal? drawer = null;
        if (shift is not null)
        {
            var sinceOpen = await TotalsAsync(companyId, restaurantId, shift.OpenedAt, ct);
            drawer = shift.OpeningCashAmount + sinceOpen.Money.Cash + sinceOpen.Deposits - sinceOpen.Withdrawals;
        }

        var paidByTill = today.Money.Cash + today.Money.Card;
        return new CashDeskSummary
        {
            CashSales = today.Money.Cash,
            CardSales = today.Money.Card,
            CreditSales = today.Money.Credit,
            ReceiptCount = today.Receipts,
            Deposits = today.Deposits,
            Withdrawals = today.Withdrawals,
            NetCashBalance = today.Money.Cash + today.Deposits - today.Withdrawals,
            HasOpenShift = shift is not null,
            CurrentDrawerBalance = drawer,
            CashPercent = SalesReportMath.Percent(today.Money.Cash, paidByTill),
            CardPercent = SalesReportMath.Percent(today.Money.Card, paidByTill)
        };
    }

    private async Task<(MoneySplit Money, int Receipts, decimal Deposits, decimal Withdrawals)> TotalsAsync(
        int companyId, int restaurantId, DateTime fromUtc, CancellationToken ct)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.CompanyId == companyId && o.RestaurantId == restaurantId && o.IsPaid && o.PaidAt >= fromUtc)
            .Select(o => new { o.Id, o.PaymentMethod, o.TotalAmount })
            .ToListAsync(ct);
        var split = await OrderPaymentSplit.ComputeAsync(
            _db, orders.Select(o => (o.Id, o.PaymentMethod, o.TotalAmount)).ToList(), ct);
        var movements = await _db.CashMovements.AsNoTracking()
            .Where(m => m.CompanyId == companyId && m.RestaurantId == restaurantId && m.CreatedAtUtc >= fromUtc)
            .Select(m => new { m.Type, m.Amount })
            .ToListAsync(ct);

        return (
            new MoneySplit(split.Values.Sum(s => s.Cash), split.Values.Sum(s => s.Card), split.Values.Sum(s => s.Credit)),
            orders.Count,
            movements.Where(m => m.Type == CashMovementType.Deposit).Sum(m => m.Amount),
            movements.Where(m => m.Type == CashMovementType.Withdrawal).Sum(m => m.Amount));
    }

    public async Task<List<CashExpenseCategoryResponse>> GetCategoriesAsync(int companyId, CancellationToken ct)
        => await _db.CashExpenseCategories.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .Select(c => new CashExpenseCategoryResponse(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<CashExpenseCategoryResponse> AddCategoryAsync(int companyId, string name, CancellationToken ct)
    {
        var value = name?.Trim() ?? string.Empty;
        if (value.Length is 0 or > 100)
            throw new BadRequestException("Kateqoriya adı 1-100 simvol olmalıdır.");
        if (await _db.CashExpenseCategories.AnyAsync(c => c.CompanyId == companyId && c.Name == value, ct))
            throw new BadRequestException("Bu adda xərc kateqoriyası artıq var.");

        var category = new CashExpenseCategory { CompanyId = companyId, Name = value };
        _db.CashExpenseCategories.Add(category);
        await _db.SaveChangesAsync(ct);
        return new CashExpenseCategoryResponse(category.Id, category.Name);
    }

    public async Task DeleteCategoryAsync(int companyId, int id, CancellationToken ct)
    {
        // Movements keep the name they were written with, so a category can go at any time.
        var category = await _db.CashExpenseCategories.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Xərc kateqoriyası tapılmadı.");
        _db.CashExpenseCategories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }
}
