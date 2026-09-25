using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;

namespace Application.Departments.Queries.GetAll;

public sealed class GetAllDepartmentsQueryHandler
    : IRequestHandler<GetAllDepartmentsQuery, BaseResponse<List<DepartmentResponse>>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllDepartmentsQueryHandler(
        IDepartmentRepository departmentRepository,
        ICurrentUserService currentUserService)
    {
        _departmentRepository = departmentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<DepartmentResponse>>> Handle(
        GetAllDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        // A tenant Admin is always confined to their own company, regardless of what companyId was
        // requested — only SuperAdmin may target an arbitrary company.
        var companyId = _currentUserService.IsSuperAdmin ? request.CompanyId : _currentUserService.CompanyId;

        var departments = await _departmentRepository.GetAllAsync(
            companyId,
            cancellationToken);

        var response = departments.Select(x => new DepartmentResponse
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            Name = x.Name,
            Description = x.Description
        }).ToList();

        return BaseResponse<List<DepartmentResponse>>.Ok(response);
    }
}