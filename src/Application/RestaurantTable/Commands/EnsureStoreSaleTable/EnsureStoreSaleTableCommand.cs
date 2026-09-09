using Application.Common.Responce;
using MediatR;

namespace Application.RestaurantTables.Commands.EnsureStoreSaleTable;

public record EnsureStoreSaleTableCommand(int RestaurantId) : IRequest<BaseResponse<int>>;
