using FluentValidation;

namespace Application.Warehouse.Queries.GetById;

public class GetWarehouseByIdQueryValidator : AbstractValidator<GetWarehouseByIdQuery>
{
    public GetWarehouseByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Warehouse id must be greater than 0.");
    }
}