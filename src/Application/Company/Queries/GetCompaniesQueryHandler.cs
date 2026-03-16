using Application.Company.Dtos.Responce;
using AutoMapper;
using MediatR;

namespace Application.Company.Queries.GetCompanies;

public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, List<GetAllCompaniesResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;

    public GetCompaniesQueryHandler(ICompanyRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<GetAllCompaniesResponse>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var companies = await _repository.GetAllAsync(cancellationToken);

        return _mapper.Map<List<GetAllCompaniesResponse>>(companies);
    }
}