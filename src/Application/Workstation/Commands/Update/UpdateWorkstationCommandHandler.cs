using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Workstation.Dtos.Responce;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Workstation.Commands.Update;

public class UpdateWorkstationCommandHandler
    : IRequestHandler<UpdateWorkstationCommand, BaseResponse<WorkstationResponse>>
{
    private readonly IWorkstationRepository _repository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateWorkstationCommandHandler> _logger;

    public UpdateWorkstationCommandHandler(
        IWorkstationRepository repository,
        IRestaurantRepository restaurantRepository,
        ICurrentUserService currentUserService,
        IMapper mapper,
        ILogger<UpdateWorkstationCommandHandler> logger)
    {
        _repository = repository;
        _restaurantRepository = restaurantRepository;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<WorkstationResponse>> Handle(
        UpdateWorkstationCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var dto = request.Request;

        var workstation = await _repository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (workstation is null)
            return BaseResponse<WorkstationResponse>.Fail("Terminal tapılmadı.");

        var name = dto.Name.Trim();
        if (await _repository.ExistsByNameAsync(companyId, name, workstation.Id, cancellationToken))
            return BaseResponse<WorkstationResponse>.Fail("Bu adda workstation artıq mövcuddur.");

        if (dto.RestaurantId.HasValue
            && !await _restaurantRepository.ExistsAsync(
                r => r.Id == dto.RestaurantId.Value && r.CompanyId == companyId, cancellationToken))
            return BaseResponse<WorkstationResponse>.Fail("Filial tapılmadı.");

        workstation.Name = name;
        workstation.RestaurantId = dto.RestaurantId;
        workstation.IsActive = dto.IsActive;

        _repository.Update(workstation);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("POS workstation updated. Id: {WorkstationId}, CompanyId: {CompanyId}", workstation.Id, companyId);

        var saved = await _repository.GetByIdAsync(workstation.Id, companyId, cancellationToken);
        return BaseResponse<WorkstationResponse>.Ok(
            _mapper.Map<WorkstationResponse>(saved ?? workstation), "Terminal yeniləndi.");
    }
}
