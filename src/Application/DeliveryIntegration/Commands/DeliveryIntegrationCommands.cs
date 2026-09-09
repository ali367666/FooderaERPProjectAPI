using Application.DeliveryIntegration.Dtos;
using MediatR;

namespace Application.DeliveryIntegration.Commands;

public record CreateDeliveryIntegrationCommand(CreateDeliveryIntegrationRequest Request) : IRequest<DeliveryIntegrationResponse>;

public record UpdateDeliveryIntegrationCommand(UpdateDeliveryIntegrationRequest Request) : IRequest<DeliveryIntegrationResponse>;

public record DeleteDeliveryIntegrationCommand(int Id) : IRequest;
