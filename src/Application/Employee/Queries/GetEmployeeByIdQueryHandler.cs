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
        var isSuperAdmin = _currentUserService.IsSuperAdmin;
        var callerCompanyId = _currentUserService.CompanyId;

        // SuperAdmin can view any company's employee; a tenant Admin is always confined to their
        // own company.
        var employee = isSuperAdmin
            ? await _employeeRepository.GetByIdAsync(request.Id, cancellationToken)
            : await _employeeRepository.GetByIdAsync(request.Id, callerCompanyId, cancellationToken);

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
            RestaurantId = employee.RestaurantId,
            RestaurantName = employee.Restaurant?.Name ?? string.Empty,
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