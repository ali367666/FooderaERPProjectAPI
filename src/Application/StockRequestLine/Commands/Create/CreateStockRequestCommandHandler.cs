using Application.Common.Interfaces.Abstracts;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.StockRequests.Commands.Create;

public class CreateStockRequestCommandHandler
    : IRequestHandler<CreateStockRequestCommand, BaseResponse<int>>
{
    private readonly IStockRequestRepository _stockRequestRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStockRequestCommandHandler(
        IStockRequestRepository stockRequestRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRequestRepository = stockRequestRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateStockRequestCommand request,
        CancellationToken cancellationToken)
    {
        var entity = new StockRequest
        {
            CompanyId = request.Request.CompanyId,
            RequestingWarehouseId = request.Request.RequestingWarehouseId,
            SupplyingWarehouseId = request.Request.SupplyingWarehouseId,
            Status = StockRequestStatus.Draft,
            Note = request.Request.Note,
            Lines = request.Request.Lines.Select(x => new StockRequestLine
            {
                CompanyId = request.Request.CompanyId,
                StockItemId = x.StockItemId,
                Quantity = x.Quantity
            }).ToList()
        };

        await _stockRequestRepository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BaseResponse<int>.Ok(entity.Id, "Stock request created successfully.");
    }
}