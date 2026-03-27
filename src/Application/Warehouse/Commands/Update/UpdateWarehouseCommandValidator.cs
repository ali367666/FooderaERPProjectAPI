using Domain.Enums;
using FluentValidation;

namespace Application.Warehouse.Commands.Update;

public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Warehouse id must be greater than 0.");

        RuleFor(x => x.Request.Name)
            .NotEmpty()
            .WithMessage("Warehouse name is required.")
            .MaximumLength(100)
            .WithMessage("Warehouse name must not exceed 100 characters.");

        RuleFor(x => x.Request.CompanyId)
            .GreaterThan(0)
            .WithMessage("CompanyId must be greater than 0.");

        RuleFor(x => x.Request.Type)
            .IsInEnum()
            .WithMessage("Invalid warehouse type.");

        RuleFor(x => x.Request)
            .Must(x => !(x.Type == WarehouseType.Restaurant && x.RestaurantId is null))
            .WithMessage("RestaurantId is required when warehouse type is Restaurant.");

        RuleFor(x => x.Request)
            .Must(x => !(x.Type == WarehouseType.Vehicle && x.DriverUserId is null))
            .WithMessage("DriverUserId is required when warehouse type is Vehicle.");

        RuleFor(x => x.Request)
            .Must(x => !(x.Type == WarehouseType.HeadOffice && (x.RestaurantId.HasValue || x.DriverUserId.HasValue)))
            .WithMessage("Main warehouse cannot have RestaurantId or DriverUserId.");

        RuleFor(x => x.Request)
            .Must(x => !(x.Type == WarehouseType.Restaurant && x.DriverUserId.HasValue))
            .WithMessage("Restaurant warehouse cannot have DriverUserId.");

        RuleFor(x => x.Request)
            .Must(x => !(x.Type == WarehouseType.Vehicle && x.RestaurantId.HasValue))
            .WithMessage("Vehicle warehouse cannot have RestaurantId.");
    }
}