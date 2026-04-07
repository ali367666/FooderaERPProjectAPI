using Application.Abstractions.Repositories;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;

namespace Application.Departments.Queries.GetAll;

public sealed class GetAllDepartmentsQueryHandler
    : IRequestHandler<GetAllDepartmentsQuery, BaseResponse<List<DepartmentResponse>>>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetAllDepartmentsQueryHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<BaseResponse<List<DepartmentResponse>>> Handle(
        GetAllDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetAllAsync(
            request.CompanyId,
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