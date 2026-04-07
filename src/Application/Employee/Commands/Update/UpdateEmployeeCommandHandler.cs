using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.Employees.Commands.Update;

public class UpdateEmployeeCommandHandler
    : IRequestHandler<UpdateEmployeeCommand, BaseResponse>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse> Handle(
        UpdateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        var employee = await _employeeRepository.GetByIdAsync(
            request.Id,
            companyId,
            cancellationToken);

        if (employee is null)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "Employee not found."
            };
        }

        // Department check
        var department = await _departmentRepository.GetByIdAsync(
            request.Request.DepartmentId,
            companyId,
            cancellationToken);

        if (department is null)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "Department not found."
            };
        }

        // Position check
        var position = await _positionRepository.GetByIdAsync(
            request.Request.PositionId,
            companyId,
            cancellationToken);

        if (position is null || position.DepartmentId != request.Request.DepartmentId)
        {
            return new BaseResponse
            {
                Success = false,
                Message = "Invalid position."
            };
        }

        // Update
        employee.FirstName = request.Request.FirstName.Trim();
        employee.LastName = request.Request.LastName.Trim();
        employee.FatherName = request.Request.FatherName;

        employee.PhoneNumber = request.Request.PhoneNumber;
        employee.Email = request.Request.Email;
        employee.Address = request.Request.Address;

        employee.HireDate = request.Request.HireDate;

        employee.DepartmentId = request.Request.DepartmentId;
        employee.PositionId = request.Request.PositionId;

        _employeeRepository.Update(employee);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse
        {
            Success = true,
            Message = "Employee updated successfully."
        };
    }
}