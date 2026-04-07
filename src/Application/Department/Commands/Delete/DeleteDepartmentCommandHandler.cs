using Application.Abstractions.Repositories;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Departments.Commands.Delete;

public sealed class DeleteDepartmentCommandHandler
    : IRequestHandler<DeleteDepartmentCommand, BaseResponse>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILogger<DeleteDepartmentCommandHandler> _logger;

    public DeleteDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        ILogger<DeleteDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var department = await _departmentRepository.GetByIdAsync(
                request.Id,
                request.CompanyId,
                cancellationToken);

            if (department is null)
                return BaseResponse.Fail("Department tapılmadı.");

            var hasPositions = await _departmentRepository.HasAnyPositionAsync(
                request.Id,
                cancellationToken);

            if (hasPositions)
                return BaseResponse.Fail("Bu department-ə bağlı position-lar var, silmək olmaz.");

            var hasEmployees = await _departmentRepository.HasAnyEmployeeAsync(
                request.Id,
                cancellationToken);

            if (hasEmployees)
                return BaseResponse.Fail("Bu department-ə bağlı employee-lər var, silmək olmaz.");

            _departmentRepository.Delete(department);
            await _departmentRepository.SaveChangesAsync(cancellationToken);

            return BaseResponse.Ok("Department uğurla silindi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "DeleteDepartmentCommand zamanı xəta baş verdi. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                request.CompanyId);

            throw;
        }
    }
}