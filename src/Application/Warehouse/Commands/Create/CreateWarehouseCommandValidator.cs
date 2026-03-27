using Domain.Enums;
using FluentValidation;

namespace Application.Warehouse.Commands.Create;

public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Warehouse name is required.")
            .MaximumLength(100).WithMessage("Warehouse name must not exceed 100 characters.");

        RuleFor(x => x.Request.CompanyId)
            .GreaterThan(0).WithMessage("Valid company id is required.");

        RuleFor(x => x.Request.Type)
            .IsInEnum().WithMessage("Warehouse type is invalid.");

        RuleFor(x => x.Request.RestaurantId)
            .NotNull()
            .When(x => x.Request.Type == WarehouseType.Restaurant)
            .WithMessage("RestaurantId is required for restaurant warehouse.");

        RuleFor(x => x.Request.DriverUserId)
            .NotNull()
            .When(x => x.Request.Type == WarehouseType.Vehicle)
            .WithMessage("DriverUserId is required for vehicle warehouse.");

        RuleFor(x => x.Request)
            .Must(request =>
                request.Type switch
                {
                    WarehouseType.HeadOffice =>
                        request.RestaurantId is null && request.DriverUserId is null,

                    WarehouseType.Restaurant =>
                        request.RestaurantId.HasValue && request.DriverUserId is null,

                    WarehouseType.Vehicle =>
                        request.DriverUserId.HasValue && request.RestaurantId is null,

                    _ => false
                })
            .WithMessage("Warehouse type does not match related entity configuration.");
    }
}