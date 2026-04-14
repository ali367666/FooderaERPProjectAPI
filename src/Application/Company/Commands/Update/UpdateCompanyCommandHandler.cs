using System.Text.Json;
using Application.Common.Extensions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Company.Commands.Update;

public sealed class UpdateCompanyCommandHandler
    : IRequestHandler<UpdateCompanyCommand, BaseResponse<UpdateCompanyResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<UpdateCompanyCommandHandler> _logger;

    public UpdateCompanyCommandHandler(
        ICompanyRepository repository,
        IAuditLogService auditLogService,
        ILogger<UpdateCompanyCommandHandler> logger)
    {
        _repository = repository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<UpdateCompanyResponse>> Handle(
        UpdateCompanyCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateCompanyCommand başladı. CompanyId: {CompanyId}",
            request.Id);

        var company = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
        {
            _logger.LogWarning(
                "Şirkət tapılmadı. Update əməliyyatı icra olunmadı. CompanyId: {CompanyId}",
                request.Id);

            return BaseResponse<UpdateCompanyResponse>.Fail("Şirkət tapılmadı.");
        }

        var dto = request.dto;

        var oldValues = JsonSerializer.Serialize(new
        {
            company.CompanyCode,
            company.Name,
            company.Description,
            company.Address,
            company.TaxNumber,
            company.TaxOfficeCode,
            company.Country,
            company.CountryCode
        });

        if (!string.IsNullOrWhiteSpace(dto.CompanyCode))
            company.CompanyCode = dto.CompanyCode;

        if (!string.IsNullOrWhiteSpace(dto.Name))
            company.Name = dto.Name;

        if (dto.Description is not null)
            company.Description = dto.Description;

        if (dto.Address is not null)
            company.Address = dto.Address;

        if (!string.IsNullOrWhiteSpace(dto.TaxNumber))
            company.TaxNumber = dto.TaxNumber;

        if (!string.IsNullOrWhiteSpace(dto.TaxOfficeCode))
            company.TaxOfficeCode = dto.TaxOfficeCode;

        if (dto.Country.HasValue)
        {
            company.Country = dto.Country.Value;
            company.CountryCode = dto.Country.Value.GetCode();
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var newValues = JsonSerializer.Serialize(new
        {
            company.CompanyCode,
            company.Name,
            company.Description,
            company.Address,
            company.TaxNumber,
            company.TaxOfficeCode,
            company.Country,
            company.CountryCode
        });

        await _auditLogService.LogAsync(
            new AuditLogEntry
            {
                EntityName = "Company",
                EntityId = company.Id.ToString(),
                ActionType = "Update",
                OldValues = oldValues,
                NewValues = newValues,
                Message = $"Company yeniləndi. Id: {company.Id}, Code: {company.CompanyCode}, Ad: {company.Name}",
                IsSuccess = true
            },
            cancellationToken);

        _logger.LogInformation(
            "Şirkət uğurla yeniləndi. CompanyId: {CompanyId}, CompanyCode: {CompanyCode}, Name: {Name}",
            company.Id,
            company.CompanyCode,
            company.Name);

        var response = new UpdateCompanyResponse
        {
            Id = company.Id,
            Message = "Şirkət uğurla yeniləndi"
        };

        return BaseResponse<UpdateCompanyResponse>.Ok(response, "Şirkət uğurla yeniləndi");
    }
}