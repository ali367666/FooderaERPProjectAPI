using Application.Company.Dtos.Responce;
using AutoMapper;
using MediatR;

namespace Application.Company.Queries.GetCompanyById;

public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, GetCompanyByIdResponse>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;

    public GetCompanyByIdQueryHandler(ICompanyRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<GetCompanyByIdResponse> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
            throw new Exception("Company tapılmadı");

        return _mapper.Map<GetCompanyByIdResponse>(company);
    }
}