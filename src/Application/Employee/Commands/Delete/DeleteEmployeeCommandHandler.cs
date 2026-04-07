using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.Employees.Commands.Delete;

public class DeleteEmployeeCommandHandler
    : IRequestHandler<DeleteEmployeeCommand, BaseResponse>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse> Handle(
        DeleteEmployeeCommand request,
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

        _employeeRepository.Delete(employee);
        await _employeeRepository.SaveChangesAsync(cancellationToken);

        return new BaseResponse
        {
            Success = true,
            Message = "Employee deleted successfully."
        };
    }
}