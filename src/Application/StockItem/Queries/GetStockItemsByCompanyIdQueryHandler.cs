using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockItem.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockItem.Queries.GetByCompanyId;

public class GetStockItemsByCompanyIdQueryHandler
    : IRequestHandler<GetStockItemsByCompanyIdQuery, BaseResponse<List<StockItemResponse>>>
{
    private readonly IStockItemRepository _stockItemRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStockItemsByCompanyIdQueryHandler> _logger;

    public GetStockItemsByCompanyIdQueryHandler(
        IStockItemRepository stockItemRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetStockItemsByCompanyIdQueryHandler> logger)
    {
        _stockItemRepository = stockItemRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<StockItemResponse>>> Handle(
        GetStockItemsByCompanyIdQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.IsSuperAdmin ? request.CompanyId : _currentUserService.CompanyId;

        _logger.LogInformation(
            "Getting stock items by company id. CompanyId: {CompanyId}",
            companyId);

        var stockItems = await _stockItemRepository
            .GetByCompanyIdAsync(companyId, cancellationToken);

        var response = _mapper.Map<List<StockItemResponse>>(stockItems);

        _logger.LogInformation(
            "Retrieved {Count} stock items for company id: {CompanyId}",
            response.Count,
            request.CompanyId);

        return BaseResponse<List<StockItemResponse>>.Ok(response);
    }
}