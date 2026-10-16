using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Commands;

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
