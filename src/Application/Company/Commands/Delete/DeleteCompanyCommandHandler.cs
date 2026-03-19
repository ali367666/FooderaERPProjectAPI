using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Commands.Delete;

public sealed class DeleteCompanyCommandHandler
    : IRequestHandler<DeleteCompanyCommand, BaseResponse<DeleteCompanyResponce>>
{
    private readonly ICompanyRepository _repository;

    public DeleteCompanyCommandHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseResponse<DeleteCompanyResponce>> Handle(
        DeleteCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
            return BaseResponse<DeleteCompanyResponce>.Fail("Şirkət tapılmadı.");

        _repository.Delete(company);
        await _repository.SaveChangesAsync(cancellationToken);

        var response = new DeleteCompanyResponce
        {
            Message = "Şirkət uğurla silindi"
        };

        return BaseResponse<DeleteCompanyResponce>.Ok(response, "Şirkət uğurla silindi");
    }
}