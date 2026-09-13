using Application.Common.Interfaces;
using Application.Company.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Company.Queries.GetCompanies;

public class GetCompaniesQueryHandler : IRequestHandler<GetCompaniesQuery, List<GetAllCompaniesResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompaniesQueryHandler> _logger;

    public GetCompaniesQueryHandler(
        ICompanyRepository repository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<GetCompaniesQueryHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<GetAllCompaniesResponse>> Handle(
        GetCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetCompaniesQuery başladı");

        // Company.View is grantable to a tenant's own role (e.g. to power the "Company filter"
        // toolbar for them) but that must never expose OTHER tenants' company records — only the
        // platform SuperAdmin gets the full cross-tenant list.
        var companies = await _repository.GetAllAsync(cancellationToken);
        if (!_currentUserService.IsSuperAdmin)
        {
            companies = companies.Where(c => c.Id == _currentUserService.CompanyId).ToList();
        }

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