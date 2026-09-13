using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Queries.GetEmployeesByPosition;

public class GetEmployeesByPositionQueryHandler
    : IRequestHandler<GetEmployeesByPositionQuery, BaseResponse<List<EmployeeResponse>>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployeesByPositionQueryHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<List<EmployeeResponse>>> Handle(
        GetEmployeesByPositionQuery request,
        CancellationToken cancellationToken)
    {
        // A tenant Admin is always confined to their own company, regardless of what companyId was
        // requested — only SuperAdmin may target an arbitrary company.
        var resolvedCompanyId = _currentUserService.IsSuperAdmin ? request.CompanyId : _currentUserService.CompanyId;

        if (!resolvedCompanyId.HasValue || resolvedCompanyId.Value <= 0)
        {
            return new BaseResponse<List<EmployeeResponse>>
            {
                Success = true,
                Message = "Employees by position retrieved successfully.",
                Data = new List<EmployeeResponse>()
            };
        }

        if (string.IsNullOrWhiteSpace(request.PositionName) && (!request.PositionId.HasValue || request.PositionId.Value <= 0))
        {
            return new BaseResponse<List<EmployeeResponse>>
            {
                Success = true,
                Message = "Employees by position retrieved successfully.",
                Data = new List<EmployeeResponse>()
            };
        }

        var companyId = resolvedCompanyId.Value;

        var employees = await _employeeRepository.GetByPositionAsync(
            companyId,
            request.PositionId,
            request.PositionName,
            cancellationToken);

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
            RestaurantId = employee.RestaurantId,
            RestaurantName = employee.Restaurant?.Name ?? string.Empty,
            UserId = employee.UserId
        }).ToList();

        return new BaseResponse<List<EmployeeResponse>>
        {
            Success = true,
            Message = "Employees by position retrieved successfully.",
            Data = response
        };
    }
}
