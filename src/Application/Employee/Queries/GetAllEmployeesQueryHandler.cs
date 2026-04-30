using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Queries.GetAll;

public class GetAllEmployeesQueryHandler
    : IRequestHandler<GetAllEmployeesQuery, BaseResponse<List<EmployeeResponse>>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAllEmployeesQueryHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<EmployeeResponse>>> Handle(
        GetAllEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var employees = await _employeeRepository.GetAllAsync(companyId, cancellationToken);

        var response = employees.Select(employee => new EmployeeResponse
        {
            Id = employee.Id,
            FullName = $"{employee.FirstName} {employee.LastName}".Trim(),
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FatherName = employee.FatherName,
            PhoneNumber = employee.PhoneNumber,
            Email = employee.Email,
            Address = employee.Address,
            HireDate = employee.HireDate,
            TerminationDate = employee.TerminationDate,
            IsActive = employee.IsActive,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            PositionId = employee.PositionId,
            PositionName = employee.Position?.Name ?? string.Empty,
            UserId = employee.UserId
        }).ToList();

        return new BaseResponse<List<EmployeeResponse>>
        {
            Success = true,
            Message = "Employees retrieved successfully.",
            Data = response
        };
    }
}