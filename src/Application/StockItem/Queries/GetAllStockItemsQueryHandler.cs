using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockItem.Dtos.Response;
using Application.StockItem.Queries;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

public class GetAllStockItemsQueryHandler
    : IRequestHandler<GetAllStockItemsQuery, BaseResponse<List<StockItemResponse>>>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllStockItemsQueryHandler> _logger;

    public GetAllStockItemsQueryHandler(
        IStockItemRepository stockItemRepository,
        IMapper mapper,
        ILogger<GetAllStockItemsQueryHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<StockItemResponse>>> Handle(
        GetAllStockItemsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all stock items with filters.");

        var stockItems = await _stockItemRepository.GetAllAsync(request.Request, cancellationToken);
        var response = _mapper.Map<List<StockItemResponse>>(stockItems);

        _logger.LogInformation("Retrieved {Count} stock items.", response.Count);

        return BaseResponse<List<StockItemResponse>>.Ok(response);
    }
}