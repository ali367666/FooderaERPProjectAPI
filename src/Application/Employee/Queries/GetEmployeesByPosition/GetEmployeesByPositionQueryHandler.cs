using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Employees.Dtos;
using MediatR;

namespace Application.Employees.Queries.GetEmployeesByPosition;

public class GetEmployeesByPositionQueryHandler
    : IRequestHandler<GetEmployeesByPositionQuery, BaseResponse<List<EmployeeResponse>>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetEmployeesByPositionQueryHandler(
        IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<BaseResponse<List<EmployeeResponse>>> Handle(
        GetEmployeesByPositionQuery request,
        CancellationToken cancellationToken)
    {
        if (!request.CompanyId.HasValue || request.CompanyId.Value <= 0)
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

        var companyId = request.CompanyId.Value;

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
