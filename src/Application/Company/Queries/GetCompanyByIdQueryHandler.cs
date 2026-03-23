using Application.Common.Responce;
using Application.Company.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Company.Queries.GetCompanyById;

public class GetCompanyByIdQueryHandler
    : IRequestHandler<GetCompanyByIdQuery, BaseResponse<GetCompanyByIdResponse>>
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompanyByIdQueryHandler> _logger;

    public GetCompanyByIdQueryHandler(
        ICompanyRepository repository,
        IMapper mapper,
        ILogger<GetCompanyByIdQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<GetCompanyByIdResponse>> Handle(
        GetCompanyByIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetCompanyByIdQuery başladı. CompanyId: {CompanyId}",
            request.Id);

        var company = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (company is null)
        {
            _logger.LogWarning(
                "Company tapılmadı. CompanyId: {CompanyId}",
                request.Id);

            return BaseResponse<GetCompanyByIdResponse>.Fail("Company tapılmadı.");
        }

        var response = _mapper.Map<GetCompanyByIdResponse>(company);

        _logger.LogInformation(
            "Company uğurla gətirildi. CompanyId: {CompanyId}, CompanyCode: {CompanyCode}, Name: {Name}",
            company.Id,
            company.CompanyCode,
            company.Name);

        return BaseResponse<GetCompanyByIdResponse>.Ok(response, "Company uğurla gətirildi.");
    }
}