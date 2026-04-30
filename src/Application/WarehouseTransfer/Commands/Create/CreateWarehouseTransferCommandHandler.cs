using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseTransfers.Commands.Create;

public class CreateWarehouseTransferCommandHandler
    : IRequestHandler<CreateWarehouseTransferCommand, BaseResponse<int>>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<CreateWarehouseTransferCommandHandler> _logger;

    public CreateWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IWarehouseRepository warehouseRepository,
        IAuditLogService auditLogService,
        ILogger<CreateWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _warehouseRepository = warehouseRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        _logger.LogInformation(
            "CreateWarehouseTransferCommand başladı. CompanyId: {CompanyId}, FromWarehouseId: {FromWarehouseId}, ToWarehouseId: {ToWarehouseId}, VehicleWarehouseId: {VehicleWarehouseId}, StockRequestId: {StockRequestId}",
            dto.CompanyId,
            dto.FromWarehouseId,
            dto.ToWarehouseId,
            dto.VehicleWarehouseId,
            dto.StockRequestId);

        if (dto.Lines is null || !dto.Lines.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer yaradılmadı. Line yoxdur. CompanyId: {CompanyId}",
                dto.CompanyId);

            return new BaseResponse<int>
            {
                Success = false,
                Message = "Transfer ən azı bir line içərməlidir."
            };
        }

        if (dto.FromWarehouseId == dto.ToWarehouseId)
        {
            _logger.LogWarning(
                "Warehouse transfer yaradılmadı. From və To warehouse eynidir. CompanyId: {CompanyId}, WarehouseId: {WarehouseId}",
                dto.CompanyId,
                dto.FromWarehouseId);

            return new BaseResponse<int>
            {
                Success = false,
                Message = "Göndərən və qəbul edən anbar eyni ola bilməz."
            };
        }

        if (dto.VehicleWarehouseId.HasValue &&
            (dto.VehicleWarehouseId.Value == dto.FromWarehouseId ||
             dto.VehicleWarehouseId.Value == dto.ToWarehouseId))
        {
            _logger.LogWarning(
                "Warehouse transfer yaradılmadı. Vehicle warehouse uyğun deyil. CompanyId: {CompanyId}, VehicleWarehouseId: {VehicleWarehouseId}",
                dto.CompanyId,
                dto.VehicleWarehouseId);

            return new BaseResponse<int>
            {
                Success = false,
                Message = "Maşın anbarı göndərən və ya qəbul edən anbarla eyni ola bilməz."
            };
        }

        if (dto.Lines.Any(x => x.Quantity <= 0))
        {
            _logger.LogWarning(
                "Warehouse transfer yaradılmadı. Quantity düzgün deyil. CompanyId: {CompanyId}",
                dto.CompanyId);

            return new BaseResponse<int>
            {
                Success = false,
                Message = "Bütün miqdarlar 0-dan böyük olmalıdır."
            };
        }

        var duplicateStockItemIds = dto.Lines
            .GroupBy(x => x.StockItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateStockItemIds.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer yaradılmadı. Duplicate StockItem var. CompanyId: {CompanyId}, DuplicateStockItemIds: {DuplicateStockItemIds}",
                dto.CompanyId,
                string.Join(", ", duplicateStockItemIds));

            return new BaseResponse<int>
            {
                Success = false,
                Message = "Eyni StockItem bir transfer daxilində bir neçə dəfə göndərilə bilməz."
            };
        }

        var fromWarehouseExists = await _warehouseRepository.ExistsAsync(
            dto.FromWarehouseId,
            cancellationToken);

        if (!fromWarehouseExists)
        {
            _logger.LogWarning(
                "Warehouse transfer yaradılmadı. From warehouse tapılmadı. WarehouseId: {WarehouseId}",
                dto.FromWarehouseId);

            return new BaseResponse<int>
            {
                Success = false,
                Message = "Göndərən anbar tapılmadı."
            };
        }

        var toWarehouseExists = await _warehouseRepository.ExistsAsync(
            dto.ToWarehouseId,
            cancellationToken);

        if (!toWarehouseExists)
        {
            _logger.LogWarning(
                "Warehouse transfer yaradılmadı. To warehouse tapılmadı. WarehouseId: {WarehouseId}",
                dto.ToWarehouseId);

            return new BaseResponse<int>
            {
                Success = false,
                Message = "Qəbul edən anbar tapılmadı."
            };
        }

        if (dto.VehicleWarehouseId.HasValue)
        {
            var vehicleWarehouseExists = await _warehouseRepository.ExistsAsync(
                dto.VehicleWarehouseId.Value,
                cancellationToken);

            if (!vehicleWarehouseExists)
            {
                _logger.LogWarning(
                    "Warehouse transfer yaradılmadı. Vehicle warehouse tapılmadı. WarehouseId: {WarehouseId}",
                    dto.VehicleWarehouseId.Value);

                return new BaseResponse<int>
                {
                    Success = false,
                    Message = "Maşın anbarı tapılmadı."
                };
            }
        }

        var stockItemIds = dto.Lines
            .Select(x => x.StockItemId)
            .Distinct()
            .ToList();

        var existingStockItemIds = await _warehouseTransferRepository
            .GetExistingStockItemIdsAsync(stockItemIds, cancellationToken);

        var missingStockItemIds = stockItemIds
            .Except(existingStockItemIds)
            .ToList();

        if (missingStockItemIds.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer yaradılmadı. Bəzi StockItem-lər tapılmadı. MissingIds: {MissingIds}",
                string.Join(", ", missingStockItemIds));

            return new BaseResponse<int>
            {
                Success = false,
                Message = $"StockItem tapılmadı: {string.Join(", ", missingStockItemIds)}"
            };
        }

        var warehouseTransfer = new Domain.Entities.WarehouseAndStock.WarehouseTransfer
        {
            CompanyId = dto.CompanyId,
            DocumentNo = Guid.NewGuid().ToString("N"),
            StockRequestId = dto.StockRequestId,
            FromWarehouseId = dto.FromWarehouseId,
            ToWarehouseId = dto.ToWarehouseId,
            VehicleWarehouseId = dto.VehicleWarehouseId,
            Note = dto.Note,
            Status = TransferStatus.Draft,
            TransferDate = DateTime.UtcNow,
            Lines = dto.Lines.Select(x => new WarehouseTransferLine
            {
                CompanyId = dto.CompanyId,
                StockItemId = x.StockItemId,
                Quantity = x.Quantity
            }).ToList()
        };

        await _warehouseTransferRepository.AddAsync(warehouseTransfer, cancellationToken);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        warehouseTransfer.DocumentNo = $"WT-{warehouseTransfer.Id:D6}";
        _warehouseTransferRepository.Update(warehouseTransfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = warehouseTransfer.Id.ToString(),
                    ActionType = "Create",
                    Message = $"WarehouseTransfer yaradıldı. Id: {warehouseTransfer.Id}, CompanyId: {warehouseTransfer.CompanyId}, StockRequestId: {warehouseTransfer.StockRequestId}, FromWarehouseId: {warehouseTransfer.FromWarehouseId}, ToWarehouseId: {warehouseTransfer.ToWarehouseId}, VehicleWarehouseId: {warehouseTransfer.VehicleWarehouseId}, Status: {warehouseTransfer.Status}, TransferDate: {warehouseTransfer.TransferDate}, Note: {warehouseTransfer.Note}, LineCount: {warehouseTransfer.Lines.Count}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "WarehouseTransfer üçün audit log yazıldı. TransferId: {TransferId}",
                warehouseTransfer.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "WarehouseTransfer create audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                warehouseTransfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla yaradıldı. TransferId: {TransferId}, CompanyId: {CompanyId}",
            warehouseTransfer.Id,
            warehouseTransfer.CompanyId);

        return new BaseResponse<int>
        {
            Success = true,
            Message = "Warehouse transfer uğurla yaradıldı.",
            Data = warehouseTransfer.Id
        };
    }
}