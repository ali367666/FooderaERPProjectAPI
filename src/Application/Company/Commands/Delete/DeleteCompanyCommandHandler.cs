using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Company.Commands.Delete;

public sealed class DeleteCompanyCommandHandler
    : IRequestHandler<DeleteCompanyCommand, BaseResponse<DeleteCompanyResponce>>
{
    private readonly ICompanyRepository _repository;
    private readonly ILogger<DeleteCompanyCommandHandler> _logger;

    public DeleteCompanyCommandHandler(
        ICompanyRepository repository,
        ILogger<DeleteCompanyCommandHandler> logger)
    {
        _repository = repository;
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

        _repository.Delete(company);
        await _repository.SaveChangesAsync(cancellationToken);

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