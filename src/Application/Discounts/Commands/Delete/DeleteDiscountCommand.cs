using MediatR;

namespace Application.Discounts.Commands.Delete;

public class DeleteDiscountCommand : IRequest
{
    public int Id { get; set; }
}
