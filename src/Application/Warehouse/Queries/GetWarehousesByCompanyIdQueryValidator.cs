using FluentValidation;

namespace Application.Warehouse.Queries.GetByCompanyId;

public class GetWarehousesByCompanyIdQueryValidator : AbstractValidator<GetWarehousesByCompanyIdQuery>
{
    public GetWarehousesByCompanyIdQueryValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0)
            .WithMessage("Company id must be greater than 0.");
    }
}