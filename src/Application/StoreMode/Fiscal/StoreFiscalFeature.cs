using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.StoreMode.Sales;
using Domain.Entities.StoreMode;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StoreMode.Fiscal;

public record StoreFiscalResult(bool Success, string? ReceiptNo, string? Error);

/// <summary>
/// The seam to a fiscal device. No device model or protocol is fixed yet (see the decision log), so the shipped
/// implementation says "not configured"; a real adapter is added by implementing this and registering it instead.
/// </summary>
public interface IStoreFiscalDevice
{
    Task<StoreFiscalResult> RegisterSaleAsync(StoreSale sale, CancellationToken ct);
}

/// <summary>Sends a finished sale to the fiscal device when the company uses one and records the answer on the sale.</summary>
public sealed class StoreFiscalService
{
    private readonly IStoreFiscalDevice _device;
    private readonly IStoreRepository _repository;
    private readonly ILogger<StoreFiscalService> _logger;

    public StoreFiscalService(IStoreFiscalDevice device, IStoreRepository repository, ILogger<StoreFiscalService> logger)
    {
        _device = device;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Never throws: a device problem leaves the sale in place with status Failed and the reason.</summary>
    public async Task RegisterAsync(StoreSale sale, bool force, CancellationToken ct)
    {
        var settings = await _repository.GetOrCreateSettingsAsync(sale.CompanyId, ct);
        if (!settings.FiscalEnabled && !force)
        {
            sale.FiscalStatus = StoreFiscalStatus.NotRequired;
            return;
        }

        sale.FiscalStatus = StoreFiscalStatus.Pending;
        try
        {
            var result = await _device.RegisterSaleAsync(sale, ct);
            sale.FiscalStatus = result.Success ? StoreFiscalStatus.Confirmed : StoreFiscalStatus.Failed;
            sale.FiscalReceiptNo = result.Success ? result.ReceiptNo : sale.FiscalReceiptNo;
            sale.FiscalError = result.Success ? null : Trim(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fiscal registration failed for sale {DocumentNo}", sale.DocumentNo);
            sale.FiscalStatus = StoreFiscalStatus.Failed;
            sale.FiscalError = Trim(ex.Message);
        }

        await _repository.SaveChangesAsync(ct);
    }

    private static string? Trim(string? v) => v is null ? null : v.Length <= 500 ? v : v[..500];
}

/// <summary>Sends a sale to the fiscal device again after a failure.</summary>
public record RetryStoreFiscalCommand(int SaleId) : IRequest<StoreSaleResponse>;

public class RetryStoreFiscalHandler : IRequestHandler<RetryStoreFiscalCommand, StoreSaleResponse>
{
    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly StoreFiscalService _fiscal;
    private readonly StoreSaleProcessor _processor;

    public RetryStoreFiscalHandler(IStoreRepository repository, ICurrentUserService currentUser, StoreFiscalService fiscal, StoreSaleProcessor processor)
    {
        _repository = repository;
        _currentUser = currentUser;
        _fiscal = fiscal;
        _processor = processor;
    }

    public async Task<StoreSaleResponse> Handle(RetryStoreFiscalCommand command, CancellationToken ct)
    {
        var sale = await _repository.GetSaleAsync(command.SaleId, _currentUser.CompanyId, ct)
            ?? throw new NotFoundException("Satış tapılmadı.");
        if (sale.Status != StoreSaleStatus.Completed)
            throw new BadRequestException("Yalnız tamamlanmış satış fiskal cihaza göndərilir.");
        if (sale.FiscalStatus == StoreFiscalStatus.Confirmed)
            throw new BadRequestException("Satış artıq fiskal təsdiq alıb.");

        await _fiscal.RegisterAsync(sale, force: true, ct);
        return await _processor.MapAsync(sale, ct);
    }
}
