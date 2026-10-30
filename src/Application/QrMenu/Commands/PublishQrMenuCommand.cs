using Application.QrMenu.Dtos;
using MediatR;

namespace Application.QrMenu.Commands;

/// <summary>"Update QR": freezes the current menu and prices as what the branch's QR codes show.</summary>
public record PublishQrMenuCommand(int RestaurantId) : IRequest<QrMenuStatusResponse>;
