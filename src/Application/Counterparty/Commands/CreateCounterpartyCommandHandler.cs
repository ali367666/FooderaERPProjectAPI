using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public class CreateCounterpartyCommandHandler : IRequestHandler<CreateCounterpartyCommand, CounterpartyResponse>
{
    private readonly ICounterpartyRepository _repository;
    private readonly ICounterpartyCategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateCounterpartyCommandHandler(
        ICounterpartyRepository repository,
        ICounterpartyCategoryRepository categoryRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CounterpartyResponse> Handle(CreateCounterpartyCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(companyId, name, null, cancellationToken))
            throw new Exception("Bu adda konturagent artıq mövcuddur.");

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, companyId, cancellationToken);
        if (category is null)
            throw new Exception("Kateqoriya tapılmadı.");

        var counterparty = new Domain.Entities.Counterparty
        {
            CompanyId = companyId,
            Name = name,
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            CategoryId = category.Id,
            IsActive = dto.IsActive,
            CurrentDebtAmount = 0
        };

        await _repository.AddAsync(counterparty, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        counterparty.Category = category;
        return Map(counterparty);
    }

    internal static CounterpartyResponse Map(Domain.Entities.Counterparty c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        PhoneNumber = c.PhoneNumber,
        CategoryId = c.CategoryId,
        CategoryName = c.Category?.Name ?? "",
        IsActive = c.IsActive,
        CurrentDebtAmount = c.CurrentDebtAmount
    };
}
