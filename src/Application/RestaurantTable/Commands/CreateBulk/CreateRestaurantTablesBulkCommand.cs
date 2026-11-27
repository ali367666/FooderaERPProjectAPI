using FluentValidation;
using MediatR;

namespace Application.RestaurantTables.Commands.CreateBulk;

public class CreateRestaurantTablesBulkRequest
{
    public int RestaurantId { get; set; }
    /// <summary>Hall the tables belong to; empty = no hall.</summary>
    public int? SectionId { get; set; }
    public int Count { get; set; }
    /// <summary>The word before the number ("Masa" gives "Masa 1"); empty = numbers only.</summary>
    public string? Prefix { get; set; }
    /// <summary>Empty = continue after the highest number already used.</summary>
    public int? StartNumber { get; set; }
    public int? Capacity { get; set; }
}

public record CreateRestaurantTablesBulkResponse(int Created, List<string> Names);

public record CreateRestaurantTablesBulkCommand(CreateRestaurantTablesBulkRequest Request)
    : IRequest<CreateRestaurantTablesBulkResponse>;

public class CreateRestaurantTablesBulkCommandValidator : AbstractValidator<CreateRestaurantTablesBulkCommand>
{
    public CreateRestaurantTablesBulkCommandValidator()
    {
        RuleFor(x => x.Request.RestaurantId)
            .GreaterThan(0).WithMessage("RestaurantId 0-dan böyük olmalıdır.");

        RuleFor(x => x.Request.Count)
            .InclusiveBetween(1, TableBulkPlanner.MaxCount)
            .WithMessage($"Masa sayı 1 ilə {TableBulkPlanner.MaxCount} arasında olmalıdır.");

        RuleFor(x => x.Request.Prefix)
            .MaximumLength(80).WithMessage("Masa adının sözü 80 simvoldan çox ola bilməz.");

        RuleFor(x => x.Request.StartNumber)
            .InclusiveBetween(1, 100000).WithMessage("Başlanğıc nömrə 1 ilə 100000 arasında olmalıdır.")
            .When(x => x.Request.StartNumber.HasValue);

        RuleFor(x => x.Request.Capacity)
            .GreaterThanOrEqualTo(0).WithMessage("Tutum mənfi ola bilməz.");
    }
}
