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

public class UpdateCounterpartyCategoryCommandHandler : IRequestHandler<UpdateCounterpartyCategoryCommand, CounterpartyCategoryResponse>
{
    private readonly ICounterpartyCategoryRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCounterpartyCategoryCommandHandler(ICounterpartyCategoryRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<CounterpartyCategoryResponse> Handle(UpdateCounterpartyCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var category = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (category is null)
            throw new Exception("Kateqoriya tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(companyId, name, category.Id, cancellationToken))
            throw new Exception("Bu adda kateqoriya artıq mövcuddur.");

        category.Name = name;
        category.IsActive = dto.IsActive;

        _repository.Update(category);
        await _repository.SaveChangesAsync(cancellationToken);

        return CreateCounterpartyCategoryCommandHandler.Map(category);
    }
}

public class DeleteCounterpartyCategoryCommandHandler : IRequestHandler<DeleteCounterpartyCategoryCommand>
{
    private readonly ICounterpartyCategoryRepository _repository;
    private readonly ICounterpartyRepository _counterpartyRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteCounterpartyCategoryCommandHandler(
        ICounterpartyCategoryRepository repository,
        ICounterpartyRepository counterpartyRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _counterpartyRepository = counterpartyRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteCounterpartyCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var category = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (category is null)
            throw new Exception("Kateqoriya tapılmadı.");

        if (await _counterpartyRepository.ExistsByCategoryIdAsync(category.Id, cancellationToken))
            throw new Exception("Bu kateqoriyanı istifadə edən konturagent(lər) var — əvvəlcə onların kateqoriyasını dəyişin.");

        _repository.Delete(category);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
