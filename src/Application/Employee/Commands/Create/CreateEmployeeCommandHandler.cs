using Application.Abstractions.Repositories;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using MediatR;

namespace Application.Employees.Commands.Create;

public class CreateEmployeeCommandHandler
    : IRequestHandler<CreateEmployeeCommand, BaseResponse<int>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<int>> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        // 🔥 1. Department check (company daxilində)
        var department = await _departmentRepository.GetByIdAsync(
            request.Request.DepartmentId,
            companyId,
            cancellationToken);

        if (department is null)
        {
            return new BaseResponse<int>
            {
                Success = false,
                Message = "Department not found."
            };
        }

        // 🔥 2. Position check (company daxilində)
        var position = await _positionRepository.GetByIdAsync(
            request.Request.PositionId,
            companyId,
            cancellationToken);

        if (position is null)
        {
            return new BaseResponse<int>
            {
                Success = false,
                Message = "Position not found."
            };
        }

        // 🔥 3. Position həmin department-ə aiddir?
        if (position.DepartmentId != request.Request.DepartmentId)
        {
            return new BaseResponse<int>
            {
                Success = false,
                Message = "Position does not belong to selected department."
            };
        }

        // 🔥 4. User check (əgər verilibsə)
        if (request.Request.UserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(
                      request.Request.UserId.Value,
                      cancellationToken);

            if (user is null || user.CompanyId != companyId)
            {
                return new BaseResponse<int>
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            // 🔥 həmin user artıq başqa employee-ə bağlıdır?
            var exists = await _employeeRepository.ExistsByUserIdAsync(
                request.Request.UserId.Value,
                cancellationToken);

            if (exists)
            {
                return new BaseResponse<int>
                {
                    Success = false,
                    Message = "User already assigned to another employee."
                };
            }
        }

        // 🔥 5. Employee yarat
        var employee = new Employee
        {
            CompanyId = companyId,
            FirstName = request.Request.FirstName.Trim(),
            LastName = request.Request.LastName.Trim(),
            FatherName = request.Request.FatherName,

            PhoneNumber = request.Request.PhoneNumber,
            Email = request.Request.Email,
            Address = request.Request.Address,

            HireDate = request.Request.HireDate,

            DepartmentId = request.Request.DepartmentId,
            PositionId = request.Request.PositionId,

            UserId = request.Request.UserId,
            IsActive = true
        };

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse<int>
        {
            Success = true,
            Message = "Employee created successfully.",
            Data = employee.Id
        };
    }
}