using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Workstation.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Workstation.Commands.Create;

public class CreateWorkstationCommandHandler
    : IRequestHandler<CreateWorkstationCommand, BaseResponse<WorkstationResponse>>
{
    private readonly IWorkstationRepository _repository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateWorkstationCommandHandler> _logger;

    public CreateWorkstationCommandHandler(
        IWorkstationRepository repository,
        IRestaurantRepository restaurantRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<CreateWorkstationCommandHandler> logger)
    {
        _repository = repository;
        _restaurantRepository = restaurantRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<WorkstationResponse>> Handle(
        CreateWorkstationCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;
        var name = dto.Name.Trim();

        if (await _repository.ExistsByNameAsync(companyId, name, null, cancellationToken))
            return BaseResponse<WorkstationResponse>.Fail("Bu adda workstation artıq mövcuddur.");

        if (dto.RestaurantId.HasValue
            && !await _restaurantRepository.ExistsAsync(
                r => r.Id == dto.RestaurantId.Value && r.CompanyId == companyId, cancellationToken))
            return BaseResponse<WorkstationResponse>.Fail("Filial tapılmadı.");

        var workstation = new Domain.Entities.Workstation

        {
            CompanyId = companyId,
            RestaurantId = dto.RestaurantId,
            Name = name,
            IsActive = dto.IsActive
        };

        await _repository.AddAsync(workstation, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("POS workstation created. Id: {WorkstationId}, CompanyId: {CompanyId}", workstation.Id, companyId);

        var saved = await _repository.GetByIdAsync(workstation.Id, companyId, cancellationToken);
        return BaseResponse<WorkstationResponse>.Ok(
            _mapper.Map<WorkstationResponse>(saved ?? workstation), "Terminal əlavə edildi.");
    }
}
