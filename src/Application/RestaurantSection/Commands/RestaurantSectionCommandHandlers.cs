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
            IsActive = dto.IsActive
        };

        await _repository.AddAsync(section, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new RestaurantSectionResponse
        {
            Id = section.Id,
            RestaurantId = section.RestaurantId,
            Name = section.Name,
            IsActive = section.IsActive
        };
    }
}

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

        _repository.Update(section);
        await _repository.SaveChangesAsync(cancellationToken);

        return new RestaurantSectionResponse
        {
            Id = section.Id,
            RestaurantId = section.RestaurantId,
            Name = section.Name,
            IsActive = section.IsActive
        };
    }
}

public class DeleteRestaurantSectionCommandHandler : IRequestHandler<DeleteRestaurantSectionCommand>
{
    private readonly IRestaurantSectionRepository _repository;
    private readonly IRestaurantTableRepository _tableRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteRestaurantSectionCommandHandler(
        IRestaurantSectionRepository repository,
        IRestaurantTableRepository tableRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _tableRepository = tableRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteRestaurantSectionCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var section = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (section is null)
            throw new Exception("Bölmə tapılmadı.");

        var linkedTables = (await _tableRepository.GetAllByRestaurantAsync(companyId, section.RestaurantId, cancellationToken))
            .Where(t => t.SectionId == section.Id)
            .ToList();
        foreach (var table in linkedTables)
        {
            table.SectionId = null;
            _tableRepository.Update(table);
        }
        if (linkedTables.Count > 0)
            await _tableRepository.SaveChangesAsync(cancellationToken);

        _repository.Delete(section);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
