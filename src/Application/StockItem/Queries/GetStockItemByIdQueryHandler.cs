using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockItem.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockItem.Queries.GetById;

public class GetStockItemByIdQueryHandler
    : IRequestHandler<GetStockItemByIdQuery, BaseResponse<StockItemResponse>>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStockItemByIdQueryHandler> _logger;

    public GetStockItemByIdQueryHandler(
        IStockItemRepository stockItemRepository,
        IMapper mapper,
        ILogger<GetStockItemByIdQueryHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<StockItemResponse>> Handle(
        GetStockItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting stock item by id. Id: {Id}", request.Id);

        var stockItem = await _stockItemRepository.GetByIdAsync(request.Id, cancellationToken);

        if (stockItem is null)
        {
            _logger.LogWarning("Stock item not found. Id: {Id}", request.Id);
            return BaseResponse<StockItemResponse>.Fail("Stock item not found.");
        }

        var response = _mapper.Map<StockItemResponse>(stockItem);

        _logger.LogInformation("Stock item retrieved successfully. Id: {Id}", request.Id);

        return BaseResponse<StockItemResponse>.Ok(response);
    }
}