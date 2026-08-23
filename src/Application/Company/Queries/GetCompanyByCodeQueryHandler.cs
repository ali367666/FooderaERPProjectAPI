using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Company.Queries.GetCompanyByCode;

public class GetCompanyByCodeQueryHandler
    : IRequestHandler<GetCompanyByCodeQuery, BaseResponse<GetCompanyByCodeResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompanyByCodeQueryHandler> _logger;

    public GetCompanyByCodeQueryHandler(
        ICompanyRepository repository,
        IMapper mapper,
        ILogger<GetCompanyByCodeQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<GetCompanyByCodeResponse>> Handle(
        GetCompanyByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var company = await _repository.GetByCompanyCodeAsync(request.CompanyCode, cancellationToken);

        if (company is null)
        {
            _logger.LogWarning("Company tapılmadı. CompanyCode: {CompanyCode}", request.CompanyCode);
            return BaseResponse<GetCompanyByCodeResponse>.Fail("Company tapılmadı.");
        }

        var response = _mapper.Map<GetCompanyByCodeResponse>(company);

        return BaseResponse<GetCompanyByCodeResponse>.Ok(response, "Company uğurla gətirildi.");
    }
}
