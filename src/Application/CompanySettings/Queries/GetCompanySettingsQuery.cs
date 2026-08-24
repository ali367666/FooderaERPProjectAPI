using Application.Common.Responce;
using Application.CompanySettings.Dtos;
using MediatR;

namespace Application.CompanySettings.Queries;

public record GetCompanySettingsQuery : IRequest<BaseResponse<CompanySettingsResponse>>;
