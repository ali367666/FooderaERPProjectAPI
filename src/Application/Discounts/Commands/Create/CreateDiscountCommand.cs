using Application.Discounts.Dtos;
using MediatR;

namespace Application.Discounts.Commands.Create;

public class CreateDiscountCommand : IRequest<DiscountResponse>
{
    public CreateDiscountRequest Request { get; set; } = default!;
}
