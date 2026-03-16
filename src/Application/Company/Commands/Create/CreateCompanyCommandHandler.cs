using AutoMapper;
using MediatR;

namespace Application.Company.Commands.Create;

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, int>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;

    public CreateCompanyCommandHandler(ICompanyRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<int> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var dto = request.dto;

        var companyCodeExists = await _repository.AnyAsync(
            x => x.CompanyCode == dto.CompanyCode,
            cancellationToken);

        if (companyCodeExists)
            throw new Exception("Bu company code artıq mövcuddur.");

        var company = _mapper.Map<Domain.Entities.Company>(dto);

        await _repository.AddAsync(company, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return company.Id;
    }
}