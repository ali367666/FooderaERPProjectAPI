using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Queries.GetById;

public class GetEmployeeByIdQueryHandler
    : IRequestHandler<GetEmployeeByIdQuery, BaseResponse<EmployeeResponse>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployeeByIdQueryHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<EmployeeResponse>> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var employee = await _employeeRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (employee is null)
        {
            return new BaseResponse<EmployeeResponse>
            {
                Success = false,
                Message = "Employee not found."
            };
        }

        var response = new EmployeeResponse
        {
            Id = employee.Id,
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
        };

        return new BaseResponse<EmployeeResponse>
        {
            Success = true,
            Message = "Employee retrieved successfully.",
            Data = response
        };
    }
}