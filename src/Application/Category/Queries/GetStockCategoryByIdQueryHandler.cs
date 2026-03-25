using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockCategory.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockCategory.Queries.GetById;

public class GetStockCategoryByIdQueryHandler
    : IRequestHandler<GetStockCategoryByIdQuery, BaseResponse<StockCategoryDetailResponse>>
{
    private readonly IStockCategoryRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStockCategoryByIdQueryHandler> _logger;

    public GetStockCategoryByIdQueryHandler(
        IStockCategoryRepository repository,
        IMapper mapper,
        ILogger<GetStockCategoryByIdQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<StockCategoryDetailResponse>> Handle(
        GetStockCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetStockCategoryById started. Id: {Id}", request.Id);

        var category = await _repository.GetByIdWithChildrenAsync(request.Id, cancellationToken);

        if (category is null)
        {
            _logger.LogWarning("Category not found. Id: {Id}", request.Id);
            return BaseResponse<StockCategoryDetailResponse>.Fail("Stock category not found.");
        }

        var response = _mapper.Map<StockCategoryDetailResponse>(category);

        return BaseResponse<StockCategoryDetailResponse>.Ok(response);
    }
}