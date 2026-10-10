using Application.Common.Exceptions;
using Application.Common.Helpers;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.License;

public class GetCompanyLicenseQueryHandler : IRequestHandler<GetCompanyLicenseQuery, CompanyLicenseDetailResponse>
{
    private readonly ILicenseRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetCompanyLicenseQueryHandler(ILicenseRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public Task<CompanyLicenseDetailResponse> Handle(GetCompanyLicenseQuery request, CancellationToken cancellationToken)
    {
        LicenseMapping.EnsureSuperAdmin(_currentUser);
        return LicenseMapping.DetailAsync(_repo, request.CompanyId, cancellationToken);
    }
}
