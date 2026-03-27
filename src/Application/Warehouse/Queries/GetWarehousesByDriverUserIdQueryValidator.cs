using FluentValidation;

namespace Application.Warehouse.Queries.GetByDriverUserId;

public class GetWarehousesByDriverUserIdQueryValidator : AbstractValidator<GetWarehousesByDriverUserIdQuery>
{
    public GetWarehousesByDriverUserIdQueryValidator()
    {
        RuleFor(x => x.DriverUserId)
            .GreaterThan(0)
            .WithMessage("Driver user id must be greater than 0.");
    }
}