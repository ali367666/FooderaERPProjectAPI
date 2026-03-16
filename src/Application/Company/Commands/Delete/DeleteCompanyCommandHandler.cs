using Application.Company.Dtos.Responce;
using MediatR;

namespace Application.Company.Commands.Delete;

public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, DeleteCompanyResponce>
{
    private readonly ICompanyRepository _repository;

    public DeleteCompanyCommandHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeleteCompanyResponce> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
            throw new Exception("Şirkət tapılmadı.");

        _repository.Delete(company);
        await _repository.SaveChangesAsync(cancellationToken);

        return new DeleteCompanyResponce
        {
            Message = "Şirkət uğurla silindi"
        };
    }
}