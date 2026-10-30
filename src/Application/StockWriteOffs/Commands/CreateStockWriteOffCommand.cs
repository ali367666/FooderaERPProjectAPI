using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities.WarehouseAndStock;
using Domain.Enums;
using MediatR;

namespace Application.StockWriteOffs.Commands;

public record CreateStockWriteOffCommand(CreateStockWriteOffRequest Request) : IRequest<BaseResponse>;

public class CreateStockWriteOffCommandHandler : IRequestHandler<CreateStockWriteOffCommand, BaseResponse>
{
    private readonly IStockWriteOffRepository _writeOffRepository;
    private readonly IStockPurchaseRepository _purchaseRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStockWriteOffCommandHandler(
        IStockWriteOffRepository writeOffRepository,
        IStockPurchaseRepository purchaseRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IStockMovementRepository stockMovementRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _writeOffRepository = writeOffRepository;
        _purchaseRepository = purchaseRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _stockMovementRepository = stockMovementRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(CreateStockWriteOffCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var companyId = _currentUserService.CompanyId;
        var userId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        if (request.Quantity <= 0)
            return Fail("Silinən miqdar 0-dan böyük olmalıdır.");
        if (!Enum.IsDefined(request.Reason))
            return Fail("Silinmə səbəbi yanlışdır.");
        if (!await _purchaseRepository.WarehouseExistsAsync(companyId, request.WarehouseId, cancellationToken))
            return Fail("Anbar tapılmadı.");

        var item = (await _purchaseRepository.GetStockItemsAsync(companyId, new[] { request.StockItemId }, cancellationToken))
            .FirstOrDefault();
        if (item is null)
            return Fail("Stok məhsulu tapılmadı.");

        if (request.StockPurchaseId is { } purchaseId)
        {
            var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
            if (purchase is null || purchase.CompanyId != companyId)
                return Fail("Seçilmiş alış tapılmadı.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var stock = await _warehouseStockRepository.GetByWarehouseAndStockItemAsync(
                companyId, request.WarehouseId, request.StockItemId, cancellationToken);
            if (stock is null || stock.Quantity < request.Quantity)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Fail($"\"{item.Name}\" anbarda kifayət qədər yoxdur (lazımdır: {request.Quantity}, var: {stock?.Quantity ?? 0}).");
            }

            stock.Quantity -= request.Quantity;
            stock.LastModifiedAtUtc = now;
            stock.LastModifiedByUserId = userId;

            var writeOff = new StockWriteOff
            {
                CompanyId = companyId,
                WarehouseId = request.WarehouseId,
                StockItemId = request.StockItemId,
                Quantity = request.Quantity,
                UnitCostAzn = item.AverageCostAzn ?? item.LastPurchasePriceAzn ?? 0m,
                Reason = request.Reason,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                StockPurchaseId = request.StockPurchaseId,
                CreatedByUserId = userId,
                CreatedAtUtc = now,
            };
            await _writeOffRepository.AddAsync(writeOff, cancellationToken);

            await _stockMovementRepository.AddAsync(
                new StockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = request.WarehouseId,
                    FromWarehouseId = request.WarehouseId,
                    ToWarehouseId = null,
                    StockItemId = request.StockItemId,
                    Type = StockMovementType.WriteOffOut,
                    SourceType = StockMovementSourceType.StockWriteOff,
                    MovementDate = now,
                    Quantity = request.Quantity,
                    Note = $"Write-off: {request.Reason}" + (writeOff.Note is null ? "" : $" — {writeOff.Note}"),
                    CreatedByUserId = userId,
                    CreatedAtUtc = now,
                },
                cancellationToken);

            await _writeOffRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new BaseResponse { Success = true, Message = "Mal anbardan silindi. Alış borcu dəyişmir." };
    }

    private static BaseResponse Fail(string message) => new() { Success = false, Message = message };
}
