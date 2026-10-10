using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Commands;

public class CreateRestaurantSectionCommandHandler : IRequestHandler<CreateRestaurantSectionCommand, RestaurantSectionResponse>
{
    private readonly IRestaurantSectionRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateRestaurantSectionCommandHandler(IRestaurantSectionRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<RestaurantSectionResponse> Handle(CreateRestaurantSectionCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(dto.RestaurantId, name, null, cancellationToken))
            throw new Exception("Bu adda bölmə artıq mövcuddur.");

        var section = new Domain.Entities.RestaurantSection
        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            Name = name,
            IsActive = dto.IsActive,
            Type = dto.Type
        };

        await _repository.AddAsync(section, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new RestaurantSectionResponse
        {
            Id = section.Id,
            RestaurantId = section.RestaurantId,
            Name = section.Name,
            IsActive = section.IsActive,
            Type = section.Type
        };
    }
}
