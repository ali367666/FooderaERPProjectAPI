using Application.RestaurantTables.Dtos;
using MediatR;

namespace Application.RestaurantTable.Commands.UpdateSection;

public record UpdateTableSectionCommand(int TableId, int? SectionId) : IRequest<RestaurantTableResponse>;
