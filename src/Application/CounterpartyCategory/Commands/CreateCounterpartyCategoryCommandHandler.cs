using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Commands;

public class CreateCounterpartyCategoryCommandHandler : IRequestHandler<CreateCounterpartyCategoryCommand, CounterpartyCategoryResponse>
{
    private readonly ICounterpartyCategoryRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateCounterpartyCategoryCommandHandler(ICounterpartyCategoryRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<CounterpartyCategoryResponse> Handle(CreateCounterpartyCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var name = request.Request.Name.Trim();

        if (await _repository.ExistsByNameAsync(companyId, name, null, cancellationToken))
            throw new Exception("Bu adda kateqoriya artıq mövcuddur.");

        var category = new Domain.Entities.CounterpartyCategory
        {
            CompanyId = companyId,
            Name = name,
            IsActive = request.Request.IsActive
        };

        await _repository.AddAsync(category, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(category);
    }

    internal static CounterpartyCategoryResponse Map(Domain.Entities.CounterpartyCategory c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        IsActive = c.IsActive
    };
}
