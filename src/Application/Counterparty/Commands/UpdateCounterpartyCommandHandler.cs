using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Counterparty.Dtos;
using MediatR;

namespace Application.Counterparty.Commands;

public class UpdateCounterpartyCommandHandler : IRequestHandler<UpdateCounterpartyCommand, CounterpartyResponse>
{
    private readonly ICounterpartyRepository _repository;
    private readonly ICounterpartyCategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCounterpartyCommandHandler(
        ICounterpartyRepository repository,
        ICounterpartyCategoryRepository categoryRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CounterpartyResponse> Handle(UpdateCounterpartyCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var counterparty = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (counterparty is null)
            throw new Exception("Konturagent tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(companyId, name, counterparty.Id, cancellationToken))
            throw new Exception("Bu adda konturagent artıq mövcuddur.");

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, companyId, cancellationToken);
        if (category is null)
            throw new Exception("Kateqoriya tapılmadı.");

        counterparty.Name = name;
        counterparty.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        counterparty.CategoryId = category.Id;
        counterparty.IsActive = dto.IsActive;

        _repository.Update(counterparty);
        await _repository.SaveChangesAsync(cancellationToken);

        counterparty.Category = category;
        return CreateCounterpartyCommandHandler.Map(counterparty);
    }
}
