using Application.Common.Extensions;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Company.Commands.Create;

public sealed class CreateCompanyCommandHandler
    : IRequestHandler<CreateCompanyCommand, BaseResponse<CreateCompanyResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCompanyCommandHandler> _logger;

    public CreateCompanyCommandHandler(
        ICompanyRepository repository,
        IAuditLogService auditLogService,
        IMapper mapper,
        ILogger<CreateCompanyCommandHandler> logger)
    {
        _repository = repository;
        _auditLogService = auditLogService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<CreateCompanyResponse>> Handle(
        CreateCompanyCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Request;

            _logger.LogInformation(
                "CreateCompanyCommand başladı. CompanyCode: {CompanyCode}, Name: {Name}",
                dto.CompanyCode,
                dto.Name);

            var companyCodeExists = await _repository.AnyAsync(
                x => x.CompanyCode == dto.CompanyCode,
                cancellationToken);

            if (companyCodeExists)
            {
                _logger.LogWarning(
                    "Company yaradılmadı. CompanyCode artıq mövcuddur. CompanyCode: {CompanyCode}",
                    dto.CompanyCode);

                return BaseResponse<CreateCompanyResponse>.Fail("Bu company code artıq mövcuddur.");
            }

            var companyNameExists = await _repository.AnyAsync(
                x => x.Name == dto.Name,
                cancellationToken);

            if (companyNameExists)
            {
                _logger.LogWarning(
                    "Company yaradılmadı. CompanyName artıq mövcuddur. Name: {Name}",
                    dto.Name);

                return BaseResponse<CreateCompanyResponse>.Fail("Bu company name artıq mövcuddur.");
            }

            var company = _mapper.Map<Domain.Entities.Company>(dto);
            company.CountryCode = dto.Country.GetCode();

            await _repository.AddAsync(company, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Company",
                    EntityId = company.Id.ToString(),
                    ActionType = "Create",
                    Message = $"Company yaradıldı. Id: {company.Id}, Code: {company.CompanyCode}, Ad: {company.Name}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "Company uğurla yaradıldı. CompanyId: {CompanyId}, CompanyCode: {CompanyCode}, Name: {Name}",
                company.Id,
                company.CompanyCode,
                company.Name);

            var response = new CreateCompanyResponse
            {
                Id = company.Id,
                Message = "Şirkət uğurla yaradıldı"
            };

            return BaseResponse<CreateCompanyResponse>.Ok(response, "Şirkət uğurla yaradıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CreateCompanyCommand zamanı xəta baş verdi. CompanyCode: {CompanyCode}, Name: {Name}",
                request.Request.CompanyCode,
                request.Request.Name);

            throw;
        }
    }
}