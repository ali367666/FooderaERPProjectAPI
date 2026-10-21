using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Commands;

/// <summary>The categories are fixed by the system (Domain.Constants.CounterpartyCategoryNames): none can be added.</summary>
public class CreateCounterpartyCategoryCommandHandler : IRequestHandler<CreateCounterpartyCategoryCommand, CounterpartyCategoryResponse>
{
    public Task<CounterpartyCategoryResponse> Handle(CreateCounterpartyCategoryCommand request, CancellationToken cancellationToken)
        => throw new Exception("Kateqoriyalar sabitdir — yenisini əlavə etmək olmaz.");

    internal static CounterpartyCategoryResponse Map(Domain.Entities.CounterpartyCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        IsActive = c.IsActive
    };
}
