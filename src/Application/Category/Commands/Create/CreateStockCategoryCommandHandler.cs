using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.StockCategory.Commands.Create;
using Application.StockCategory.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockCategory.Commands.Create;

public class CreateStockCategoryCommandHandler
    : IRequestHandler<CreateStockCategoryCommand, BaseResponse<CreateStockCategoryResponse>>
{
    private readonly IStockCategoryRepository _stockCategoryRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateStockCategoryCommandHandler> _logger;

    public CreateStockCategoryCommandHandler(
        IStockCategoryRepository stockCategoryRepository,
        ICompanyRepository companyRepository,
        IMapper mapper,
        ILogger<CreateStockCategoryCommandHandler> logger)
    {
        _stockCategoryRepository = stockCategoryRepository;
        _companyRepository = companyRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<CreateStockCategoryResponse>> Handle(
        CreateStockCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        _logger.LogInformation(
            "CreateStockCategoryCommand started. Name: {Name}, CompanyId: {CompanyId}, ParentId: {ParentId}",
            dto.Name,
            dto.CompanyId,
            dto.ParentId);

        var companyExists = await _companyRepository.ExistsAsync(dto.CompanyId, cancellationToken);
        if (!companyExists)
        {
            _logger.LogWarning(
                "CreateStockCategoryCommand failed. Company not found. CompanyId: {CompanyId}",
                dto.CompanyId);

            return BaseResponse<CreateStockCategoryResponse>.Fail("Company not found.");
        }

        var normalizedName = dto.Name.Trim();

        var nameExists = await _stockCategoryRepository.ExistsByNameAsync(
            normalizedName,
            dto.CompanyId,
            cancellationToken);

        if (nameExists)
        {
            _logger.LogWarning(
                "CreateStockCategoryCommand failed. Duplicate category name. Name: {Name}, CompanyId: {CompanyId}",
                normalizedName,
                dto.CompanyId);

            return BaseResponse<CreateStockCategoryResponse>.Fail("Category name already exists for this company.");
        }

        Domain.Entities.WarehouseAndStock.StockCategory? parentCategory = null;

        if (dto.ParentId.HasValue)
        {
            parentCategory = await _stockCategoryRepository.GetByIdAsync(dto.ParentId.Value, cancellationToken);

            if (parentCategory is null)
            {
                _logger.LogWarning(
                    "CreateStockCategoryCommand failed. Parent category not found. ParentId: {ParentId}",
                    dto.ParentId.Value);

                return BaseResponse<CreateStockCategoryResponse>.Fail("Parent category not found.");
            }

            if (parentCategory.CompanyId != dto.CompanyId)
            {
                _logger.LogWarning(
                    "CreateStockCategoryCommand failed. Parent category does not belong to company. ParentId: {ParentId}, CompanyId: {CompanyId}",
                    dto.ParentId.Value,
                    dto.CompanyId);

                return BaseResponse<CreateStockCategoryResponse>.Fail("Parent category does not belong to the selected company.");
            }
        }

        Domain.Entities.WarehouseAndStock.StockCategory stockCategory = new Domain.Entities.WarehouseAndStock.StockCategory
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IsActive = dto.IsActive,
            CompanyId = dto.CompanyId,
            ParentId = dto.ParentId
        };

        await _stockCategoryRepository.AddAsync(stockCategory, cancellationToken);
        await _stockCategoryRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stock category created successfully. Id: {Id}, Name: {Name}, CompanyId: {CompanyId}",
            stockCategory.Id,
            stockCategory.Name,
            stockCategory.CompanyId);

        var response = _mapper.Map<CreateStockCategoryResponse>(stockCategory);

        return BaseResponse<CreateStockCategoryResponse>.Ok(
            response,
            "Stock category created successfully.");
    }
}