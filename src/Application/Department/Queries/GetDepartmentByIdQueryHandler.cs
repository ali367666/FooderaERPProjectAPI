using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;

namespace Application.Departments.Queries.GetById;

public sealed class GetDepartmentByIdQueryHandler
    : IRequestHandler<GetDepartmentByIdQuery, BaseResponse<DepartmentResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetDepartmentByIdQueryHandler(
        IDepartmentRepository departmentRepository,
        ICurrentUserService currentUserService)
    {
        _departmentRepository = departmentRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<DepartmentResponse>> Handle(
        GetDepartmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.IsSuperAdmin ? request.CompanyId : _currentUserService.CompanyId;

        var department = await _departmentRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (department is null)
            return BaseResponse<DepartmentResponse>.Fail("Department tapılmadı.");

        var response = new DepartmentResponse
        {
            Id = department.Id,
            CompanyId = department.CompanyId,
            Name = department.Name,
            Description = department.Description
        };

        return BaseResponse<DepartmentResponse>.Ok(response);
    }
}