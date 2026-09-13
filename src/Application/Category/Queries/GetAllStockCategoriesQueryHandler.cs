using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockCategory.Commands;
using Application.StockCategory.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockCategory.Queries.GetAll;

public class GetAllStockCategoriesQueryHandler
    : IRequestHandler<GetAllStockCategoriesQuery, BaseResponse<List<StockCategoryResponse>>>
{
    private readonly IStockCategoryRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllStockCategoriesQueryHandler> _logger;

    public GetAllStockCategoriesQueryHandler(
        IStockCategoryRepository repository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetAllStockCategoriesQueryHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<List<StockCategoryResponse>>> Handle(
        GetAllStockCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllStockCategories started.");

        var data = await _repository.GetAllAsync(cancellationToken);

        // A tenant Admin only ever sees their own company's categories, regardless of what
        // CompanyId was requested — only SuperAdmin may target an arbitrary company (or omit it to
        // browse every company).
        if (!_currentUserService.IsSuperAdmin)
        {
            data = data.Where(x => x.CompanyId == _currentUserService.CompanyId).ToList();
        }
        else if (request.Request.CompanyId.HasValue)
            data = data.Where(x => x.CompanyId == request.Request.CompanyId.Value).ToList();

        if (request.Request.IsActive.HasValue)
            data = data.Where(x => x.IsActive == request.Request.IsActive.Value).ToList();

        if (request.Request.ParentId.HasValue)
            data = data.Where(x => x.ParentId == request.Request.ParentId.Value).ToList();

        if (!string.IsNullOrWhiteSpace(request.Request.SearchTerm))
            data = data.Where(x => x.Name.ToLower().Contains(request.Request.SearchTerm.ToLower())).ToList();

        var response = _mapper.Map<List<StockCategoryResponse>>(data);

        return BaseResponse<List<StockCategoryResponse>>.Ok(response);
    }
}