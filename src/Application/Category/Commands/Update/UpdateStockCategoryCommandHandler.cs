using System.Text.Json;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Application.StockCategory.Dtos.Response;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.StockCategory.Commands.Update;

public class UpdateStockCategoryCommandHandler
    : IRequestHandler<UpdateStockCategoryCommand, BaseResponse<CreateStockCategoryResponse>>
{
    private readonly IStockCategoryRepository _stockCategoryRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateStockCategoryCommandHandler> _logger;

    public UpdateStockCategoryCommandHandler(
        IStockCategoryRepository stockCategoryRepository,
        ICompanyRepository companyRepository,
        IAuditLogService auditLogService,
        IMapper mapper,
        ILogger<UpdateStockCategoryCommandHandler> logger)
    {
        _stockCategoryRepository = stockCategoryRepository;
        _companyRepository = companyRepository;
        _auditLogService = auditLogService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<CreateStockCategoryResponse>> Handle(
        UpdateStockCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        _logger.LogInformation(
            "UpdateStockCategoryCommand started. Id: {Id}, Name: {Name}, CompanyId: {CompanyId}",
            request.Id,
            dto.Name,
            dto.CompanyId);

        var stockCategory = await _stockCategoryRepository.GetByIdAsync(request.Id, cancellationToken);

        if (stockCategory is null)
        {
            _logger.LogWarning(
                "UpdateStockCategoryCommand failed. Category not found. Id: {Id}",
                request.Id);

            return BaseResponse<CreateStockCategoryResponse>.Fail("Stock category not found.");
        }

        var companyExists = await _companyRepository.ExistsAsync(dto.CompanyId, cancellationToken);
        if (!companyExists)
        {
            _logger.LogWarning(
                "UpdateStockCategoryCommand failed. Company not found. CompanyId: {CompanyId}",
                dto.CompanyId);

            return BaseResponse<CreateStockCategoryResponse>.Fail("Company not found.");
        }

        var normalizedName = dto.Name.Trim();

        var nameExists = await _stockCategoryRepository.ExistsByNameAsync(
            normalizedName,
            dto.CompanyId,
            cancellationToken);

        if (nameExists && !string.Equals(stockCategory.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "UpdateStockCategoryCommand failed. Duplicate category name. Name: {Name}, CompanyId: {CompanyId}",
                normalizedName,
                dto.CompanyId);

            return BaseResponse<CreateStockCategoryResponse>.Fail("Category name already exists for this company.");
        }

        if (dto.ParentId.HasValue)
        {
            if (dto.ParentId.Value == stockCategory.Id)
                return BaseResponse<CreateStockCategoryResponse>.Fail("Category özünü parent seçə bilməz.");

            var parentCategory = await _stockCategoryRepository.GetByIdAsync(dto.ParentId.Value, cancellationToken);

            if (parentCategory is null)
            {
                _logger.LogWarning(
                    "UpdateStockCategoryCommand failed. Parent category not found. ParentId: {ParentId}",
                    dto.ParentId.Value);

                return BaseResponse<CreateStockCategoryResponse>.Fail("Parent category not found.");
            }

            if (parentCategory.CompanyId != dto.CompanyId)
            {
                _logger.LogWarning(
                    "UpdateStockCategoryCommand failed. Parent category does not belong to company. ParentId: {ParentId}, CompanyId: {CompanyId}",
                    dto.ParentId.Value,
                    dto.CompanyId);

                return BaseResponse<CreateStockCategoryResponse>.Fail("Parent category does not belong to the selected company.");
            }
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            stockCategory.Name,
            stockCategory.Description,
            stockCategory.IsActive,
            stockCategory.ParentId
        });

        stockCategory.Name = normalizedName;
        stockCategory.Description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();
        stockCategory.IsActive = dto.IsActive;
        stockCategory.ParentId = dto.ParentId;
        stockCategory.CompanyId = dto.CompanyId;

        _stockCategoryRepository.Update(stockCategory);
        await _stockCategoryRepository.SaveChangesAsync(cancellationToken);

        var newValues = JsonSerializer.Serialize(new
        {
            stockCategory.Name,
            stockCategory.Description,
            stockCategory.IsActive,
            stockCategory.ParentId
        });

        await _auditLogService.LogAsync(
            new AuditLogEntry
            {
                EntityName = "StockCategory",
                EntityId = stockCategory.Id.ToString(),
                ActionType = "Update",
                OldValues = oldValues,
                NewValues = newValues,
                Message = $"Stock category update edildi. Id: {stockCategory.Id}, Ad: {stockCategory.Name}",
                IsSuccess = true
            },
            cancellationToken);

        _logger.LogInformation(
            "Stock category updated successfully. Id: {Id}, Name: {Name}",
            stockCategory.Id,
            stockCategory.Name);

        var response = _mapper.Map<CreateStockCategoryResponse>(stockCategory);

        return BaseResponse<CreateStockCategoryResponse>.Ok(
            response,
            "Stock category updated successfully.");
    }
}