using Application.Discounts.Dtos;
using MediatR;

namespace Application.Discounts.Commands.Update;

public class UpdateDiscountCommand : IRequest<DiscountResponse>
{
    public int Id { get; set; }
    public UpdateDiscountRequest Request { get; set; } = default!;
}
