using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Commands;

public class UpdateRestaurantSectionCommandHandler : IRequestHandler<UpdateRestaurantSectionCommand, RestaurantSectionResponse>
{
    private readonly IRestaurantSectionRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateRestaurantSectionCommandHandler(IRestaurantSectionRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<RestaurantSectionResponse> Handle(UpdateRestaurantSectionCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var section = await _repository.GetByIdAsync(dto.Id, companyId, cancellationToken);
        if (section is null)
            throw new Exception("Bölmə tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(section.RestaurantId, name, section.Id, cancellationToken))
            throw new Exception("Bu adda bölmə artıq mövcuddur.");

        section.Name = name;
        section.IsActive = dto.IsActive;
        section.Type = dto.Type;

        _repository.Update(section);
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
