using Application.Common.Interfaces;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllStockItemsQueryHandler> _logger;

    public GetAllStockItemsQueryHandler(
        IStockItemRepository stockItemRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetAllStockItemsQueryHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<StockItemResponse>>> Handle(
        GetAllStockItemsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all stock items with filters.");

        // A tenant Admin only ever sees their own company's stock items, regardless of what
        // CompanyId was requested — only SuperAdmin may target an arbitrary company (or omit it to
        // browse every company).
        if (!_currentUserService.IsSuperAdmin)
        {
            request.Request.CompanyId = _currentUserService.CompanyId;
        }

        var stockItems = await _stockItemRepository.GetAllAsync(request.Request, cancellationToken);
        var response = _mapper.Map<List<StockItemResponse>>(stockItems);

        _logger.LogInformation("Retrieved {Count} stock items.", response.Count);

        return BaseResponse<List<StockItemResponse>>.Ok(response);
    }
}