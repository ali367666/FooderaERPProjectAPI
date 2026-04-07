using Application.Abstractions.Repositories;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Departments.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Departments.Commands.Update;

public sealed class UpdateDepartmentCommandHandler
    : IRequestHandler<UpdateDepartmentCommand, BaseResponse<DepartmentResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILogger<UpdateDepartmentCommandHandler> _logger;

    public UpdateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        ILogger<UpdateDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<DepartmentResponse>> Handle(
        UpdateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Request.Name))
                return BaseResponse<DepartmentResponse>.Fail("Department adı boş ola bilməz.");

            var department = await _departmentRepository.GetByIdAsync(
                request.Id,
                request.CompanyId,
                cancellationToken);

            if (department is null)
                return BaseResponse<DepartmentResponse>.Fail("Department tapılmadı.");

            var trimmedName = request.Request.Name.Trim();

            if (!string.Equals(department.Name, trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _departmentRepository.ExistsByNameAsync(
                    request.CompanyId,
                    trimmedName,
                    cancellationToken);

                if (exists)
                    return BaseResponse<DepartmentResponse>.Fail("Bu adda department artıq mövcuddur.");
            }

            department.Name = trimmedName;
            department.Description = request.Request.Description?.Trim();

            _departmentRepository.Update(department);
            await _departmentRepository.SaveChangesAsync(cancellationToken);

            var response = new DepartmentResponse
            {
                Id = department.Id,
                CompanyId = department.CompanyId,
                Name = department.Name,
                Description = department.Description
            };

            return BaseResponse<DepartmentResponse>.Ok(response, "Department uğurla yeniləndi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UpdateDepartmentCommand zamanı xəta baş verdi. Id: {Id}, CompanyId: {CompanyId}",
                request.Id,
                request.CompanyId);

            throw;
        }
    }
}