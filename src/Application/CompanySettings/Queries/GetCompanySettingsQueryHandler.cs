using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.CompanySettings.Dtos;
using AutoMapper;
using MediatR;

namespace Application.CompanySettings.Queries;

public class GetCompanySettingsQueryHandler
    : IRequestHandler<GetCompanySettingsQuery, BaseResponse<CompanySettingsResponse>>
{
    private readonly ICompanySettingsRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetCompanySettingsQueryHandler(
        ICompanySettingsRepository repository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<BaseResponse<CompanySettingsResponse>> Handle(
        GetCompanySettingsQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var settings = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);

        if (settings is null)
        {
            settings = new Domain.Entities.CompanySettings
            {
                CompanyId = companyId
            };

            await _repository.AddAsync(settings, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        var response = _mapper.Map<CompanySettingsResponse>(settings);
        return BaseResponse<CompanySettingsResponse>.Ok(response);
    }
}
