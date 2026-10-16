using Application.DeliveryIntegration.Dtos;
using MediatR;

namespace Application.DeliveryIntegration.Commands;

public record DeleteDeliveryIntegrationCommand(int Id) : IRequest;
