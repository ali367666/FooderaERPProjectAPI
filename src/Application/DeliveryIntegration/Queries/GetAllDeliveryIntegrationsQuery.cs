using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.DeliveryIntegration.Commands;
using Application.DeliveryIntegration.Dtos;
using MediatR;

namespace Application.DeliveryIntegration.Queries;

public record GetAllDeliveryIntegrationsQuery(int RestaurantId) : IRequest<List<DeliveryIntegrationResponse>>;
