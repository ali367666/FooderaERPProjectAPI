using Application.Common.Responce;
using MediatR;

namespace Application.CompanySettings.Commands.SetModules;

public record SetCompanyModulesCommand(int CompanyId, SetCompanyModulesRequest Request) : IRequest<BaseResponse>;
