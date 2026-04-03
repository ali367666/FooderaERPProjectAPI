using Application.WarehouseTransfer.Dtos.Request;
using Application.WarehouseTransfers.Dtos.Request;
using FluentValidation;

namespace Application.WarehouseTransfers.Commands.Create;

public class CreateWarehouseTransferCommandValidator : AbstractValidator<CreateWarehouseTransferCommand>
{
    public CreateWarehouseTransferCommandValidator()
    {
        RuleFor(x => x.Request.CompanyId)
            .GreaterThan(0);

        RuleFor(x => x.Request.FromWarehouseId)
            .GreaterThan(0);

        RuleFor(x => x.Request.ToWarehouseId)
            .GreaterThan(0);

        RuleFor(x => x.Request)
            .Must(x => x.FromWarehouseId != x.ToWarehouseId)
            .WithMessage("From warehouse və To warehouse eyni ola bilməz.");

        RuleFor(x => x.Request.Lines)
            .NotEmpty()
            .WithMessage("Ən azı 1 line olmalıdır.");

        RuleForEach(x => x.Request.Lines)
            .SetValidator(new CreateWarehouseTransferLineRequestValidator());
    }
}

public class CreateWarehouseTransferLineRequestValidator : AbstractValidator<CreateWarehouseTransferLineRequest>
{
    public CreateWarehouseTransferLineRequestValidator()
    {
        RuleFor(x => x.StockItemId)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}