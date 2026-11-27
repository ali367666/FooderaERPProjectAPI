using Application.PublicMenu.Dtos;
using MediatR;

namespace Application.PublicMenu.Queries;

/// <param name="TableToken">The token in a table's own QR code; makes the response say which table scanned it.</param>
public record GetPublicMenuQuery(int RestaurantId, string? TableToken = null) : IRequest<PublicMenuResponse?>;
