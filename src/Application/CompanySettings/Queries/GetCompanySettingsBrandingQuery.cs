using Application.Common.Responce;
using Application.CompanySettings.Dtos;
using MediatR;

namespace Application.CompanySettings.Queries;

public record GetCompanySettingsBrandingQuery(int CompanyId, int? RestaurantId = null)
    : IRequest<BaseResponse<CompanySettingsBrandingResponse>>;
