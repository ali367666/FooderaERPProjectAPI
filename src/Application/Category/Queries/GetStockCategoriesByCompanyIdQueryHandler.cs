using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockCategory.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockCategory.Queries.GetByCompanyId;

public class GetStockCategoriesByCompanyIdQueryHandler
    : IRequestHandler<GetStockCategoriesByCompanyIdQuery, BaseResponse<List<StockCategoryResponse>>>
{
    private readonly IStockCategoryRepository _stockCategoryRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStockCategoriesByCompanyIdQueryHandler> _logger;

    public GetStockCategoriesByCompanyIdQueryHandler(
        IStockCategoryRepository stockCategoryRepository,
        IMapper mapper,
        ILogger<GetStockCategoriesByCompanyIdQueryHandler> logger)
    {
        _stockCategoryRepository = stockCategoryRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<StockCategoryResponse>>> Handle(
        GetStockCategoriesByCompanyIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetStockCategoriesByCompanyIdQuery started. CompanyId: {CompanyId}",
            request.CompanyId);

        var categories = await _stockCategoryRepository.GetByCompanyIdAsync(
            request.CompanyId,
            cancellationToken);

        if (categories is null || categories.Count == 0)
        {
            _logger.LogWarning(
                "No stock categories found for CompanyId: {CompanyId}",
                request.CompanyId);

            return BaseResponse<List<StockCategoryResponse>>.Fail(
                "No stock categories found for this company.");
        }

        var mappedCategories = _mapper.Map<List<StockCategoryResponse>>(categories);

        _logger.LogInformation(
            "GetStockCategoriesByCompanyIdQuery completed successfully. CompanyId: {CompanyId}, Count: {Count}",
            request.CompanyId,
            mappedCategories.Count);

        return BaseResponse<List<StockCategoryResponse>>.Ok(
            mappedCategories,
            "Stock categories retrieved successfully.");
    }
}