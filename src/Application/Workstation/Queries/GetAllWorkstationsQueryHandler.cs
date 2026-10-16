using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Workstation.Dtos.Responce;
using AutoMapper;
using MediatR;

namespace Application.Workstation.Queries;

public class GetAllWorkstationsQueryHandler
    : IRequestHandler<GetAllWorkstationsQuery, BaseResponse<List<WorkstationResponse>>>
{
    private readonly IWorkstationRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetAllWorkstationsQueryHandler(
        IWorkstationRepository repository,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<WorkstationResponse>>> Handle(
        GetAllWorkstationsQuery request,
        CancellationToken cancellationToken)
    {
        var workstations = await _repository.GetAllByCompanyAsync(
            _currentUserService.CompanyId, request.RestaurantId, cancellationToken);

        return BaseResponse<List<WorkstationResponse>>.Ok(
            _mapper.Map<List<WorkstationResponse>>(workstations), "Terminallar gətirildi.");
    }
}
