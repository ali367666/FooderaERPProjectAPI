using Application.DeliveryIntegration.Dtos;
using MediatR;

namespace Application.DeliveryIntegration.Commands;

public record UpdateDeliveryIntegrationCommand(UpdateDeliveryIntegrationRequest Request) : IRequest<DeliveryIntegrationResponse>;
