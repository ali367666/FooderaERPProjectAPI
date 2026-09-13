using Application.Common.Responce;
using Application.CompanySettings.Dtos;
using MediatR;

namespace Application.CompanySettings.Commands.Update;

public record UpdateCompanySettingsCommand(UpdateCompanySettingsRequest Request, int? CompanyId = null)
    : IRequest<BaseResponse<CompanySettingsResponse>>;
