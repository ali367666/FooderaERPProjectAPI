using System.Text.Json;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Company.Commands.Delete;

public sealed class DeleteCompanyCommandHandler
    : IRequestHandler<DeleteCompanyCommand, BaseResponse<DeleteCompanyResponce>>
{
    private readonly ICompanyRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteCompanyCommandHandler> _logger;

    public DeleteCompanyCommandHandler(
        ICompanyRepository repository,
        IAuditLogService auditLogService,
        ILogger<DeleteCompanyCommandHandler> logger)
    {
        _repository = repository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse<DeleteCompanyResponce>> Handle(
        DeleteCompanyCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteCompanyCommand başladı. CompanyId: {CompanyId}",
            request.Id);

        var company = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
        {
            _logger.LogWarning(
                "Şirkət tapılmadı. Delete əməliyyatı icra olunmadı. CompanyId: {CompanyId}",
                request.Id);

            return BaseResponse<DeleteCompanyResponce>.Fail("Şirkət tapılmadı.");
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            company.Id,
            company.CompanyCode,
            company.Name
        });

        try
        {
            _repository.Delete(company);
            await _repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Şirkət silinərkən DB xətası baş verdi. CompanyId: {CompanyId}",
                company.Id);

            if (ex.InnerException?.Message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) == true)
            {
                return BaseResponse<DeleteCompanyResponce>.Fail(
                    "Bu şirkət silinə bilməz, çünki ona bağlı məlumatlar var."
                );
            }

            return BaseResponse<DeleteCompanyResponce>.Fail(
                "Şirkət silinərkən gözlənilməz verilənlər bazası xətası baş verdi."
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Şirkət silinərkən gözlənilməz xəta baş verdi. CompanyId: {CompanyId}",
                company.Id);

            return BaseResponse<DeleteCompanyResponce>.Fail(
                "Şirkət silinərkən gözlənilməz xəta baş verdi."
            );
        }

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "Company",
                    EntityId = company.Id.ToString(),
                    ActionType = "Delete",
                    OldValues = oldValues,
                    NewValues = null,
                    Message = $"Company silindi. Id: {company.Id}, Code: {company.CompanyCode}, Ad: {company.Name}",
                    IsSuccess = true
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Company delete üçün audit log yazılarkən xəta baş verdi. CompanyId: {CompanyId}",
                company.Id);
        }

        _logger.LogInformation(
            "Şirkət uğurla silindi. CompanyId: {CompanyId}, CompanyCode: {CompanyCode}, Name: {Name}",
            company.Id,
            company.CompanyCode,
            company.Name);

        var response = new DeleteCompanyResponce
        {
            Message = "Şirkət uğurla silindi"
        };

        return BaseResponse<DeleteCompanyResponce>.Ok(response, "Şirkət uğurla silindi");
    }
}