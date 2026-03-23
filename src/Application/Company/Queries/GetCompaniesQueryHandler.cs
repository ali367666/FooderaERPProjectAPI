using Application.Company.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Company.Queries.GetCompanies;

public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, List<GetAllCompaniesResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompaniesQueryHandler> _logger;

    public GetCompaniesQueryHandler(
        ICompanyRepository repository,
        IMapper mapper,
        ILogger<GetCompaniesQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<GetAllCompaniesResponse>> Handle(
        GetCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetCompaniesQuery başladı");

        var companies = await _repository.GetAllAsync(cancellationToken);

        if (companies is null || !companies.Any())
        {
            _logger.LogInformation("Heç bir company tapılmadı");
            return new List<GetAllCompaniesResponse>();
        }

        _logger.LogInformation(
            "Company list uğurla gətirildi. Count: {Count}",
            companies.Count);

        return _mapper.Map<List<GetAllCompaniesResponse>>(companies);
    }
}