using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.RestaurantSection.Dtos;
using MediatR;

namespace Application.RestaurantSection.Commands;

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
