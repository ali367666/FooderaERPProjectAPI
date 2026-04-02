using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.StockRequests.Commands.Update;

public class UpdateStockRequestCommandHandler
    : IRequestHandler<UpdateStockRequestCommand, BaseResponse>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse> Handle(
        UpdateStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _stockRequestRepository.GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (entity is null)
            return BaseResponse.Fail("Stock request not found.");

        if (entity.Status != StockRequestStatus.Draft)
            return BaseResponse.Fail("Only draft requests can be updated.");

        entity.RequestingWarehouseId = request.Request.RequestingWarehouseId;
        entity.SupplyingWarehouseId = request.Request.SupplyingWarehouseId;
        entity.Note = request.Request.Note;

        var incomingLines = request.Request.Lines;

        var incomingLineIds = incomingLines
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        var existingLines = entity.Lines.ToList();

        var linesToRemove = existingLines
            .Where(x => !incomingLineIds.Contains(x.Id))
            .ToList();

        foreach (var line in linesToRemove)
        {
            entity.Lines.Remove(line);
        }

        foreach (var incomingLine in incomingLines)
        {
            if (incomingLine.Id.HasValue)
            {
                var existingLine = existingLines
                    .FirstOrDefault(x => x.Id == incomingLine.Id.Value);

                if (existingLine is null)
                    return BaseResponse.Fail($"Stock request line with id {incomingLine.Id.Value} not found.");

                existingLine.StockItemId = incomingLine.StockItemId;
                existingLine.Quantity = incomingLine.Quantity;
            }
            else
            {
                entity.Lines.Add(new Domain.Entities.StockRequestLine
                {
                    CompanyId = entity.CompanyId,
                    StockItemId = incomingLine.StockItemId,
                    Quantity = incomingLine.Quantity
                });
            }
        }

        _stockRequestRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BaseResponse.Ok("Stock request updated successfully.");
    }
}