using FluentValidation;

namespace Application.Warehouse.Queries.GetByRestaurantId;

public class GetWarehousesByRestaurantIdQueryValidator : AbstractValidator<GetWarehousesByRestaurantIdQuery>
{
    public GetWarehousesByRestaurantIdQueryValidator()
    {
        RuleFor(x => x.RestaurantId)
            .GreaterThan(0)
            .WithMessage("Restaurant id must be greater than 0.");
    }
}