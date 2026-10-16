using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.CounterpartyCategory.Dtos;
using MediatR;

namespace Application.CounterpartyCategory.Commands;

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
