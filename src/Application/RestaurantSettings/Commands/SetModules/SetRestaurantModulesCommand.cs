using Application.Common.Responce;
using MediatR;

namespace Application.RestaurantSettings.Commands.SetModules;

public record SetRestaurantModulesCommand(int RestaurantId, SetRestaurantModulesRequest Request) : IRequest<BaseResponse>;
