using Application.Abstractions.Repositories;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Application.Departments.Dtos;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Departments.Commands.Create;

public sealed class CreateDepartmentCommandHandler
    : IRequestHandler<CreateDepartmentCommand, BaseResponse<DepartmentResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILogger<CreateDepartmentCommandHandler> _logger;

    public CreateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        ILogger<CreateDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _logger = logger;
    }

    public async Task<BaseResponse<DepartmentResponse>> Handle(
        CreateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Request;

            _logger.LogInformation(
                "CreateDepartmentCommand başladı. CompanyId: {CompanyId}, Name: {Name}",
                dto.CompanyId,
                dto.Name);

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BaseResponse<DepartmentResponse>.Fail("Department adı boş ola bilməz.");

            var exists = await _departmentRepository.ExistsByNameAsync(
                dto.CompanyId,
                dto.Name.Trim(),
                cancellationToken);

            if (exists)
            {
                _logger.LogWarning(
                    "Department yaradılmadı. Eyni adda department artıq mövcuddur. CompanyId: {CompanyId}, Name: {Name}",
                    dto.CompanyId,
                    dto.Name);

                return BaseResponse<DepartmentResponse>.Fail("Bu adda department artıq mövcuddur.");
            }

            var department = new Department
            {
                CompanyId = dto.CompanyId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim()
            };

            await _departmentRepository.AddAsync(department, cancellationToken);
            await _departmentRepository.SaveChangesAsync(cancellationToken);

            var response = new DepartmentResponse
            {
                Id = department.Id,
                CompanyId = department.CompanyId,
                Name = department.Name,
                Description = department.Description
            };

            _logger.LogInformation(
                "Department uğurla yaradıldı. DepartmentId: {DepartmentId}, CompanyId: {CompanyId}, Name: {Name}",
                department.Id,
                department.CompanyId,
                department.Name);

            return BaseResponse<DepartmentResponse>.Ok(response, "Department uğurla yaradıldı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "CreateDepartmentCommand zamanı xəta baş verdi. CompanyId: {CompanyId}, Name: {Name}",
                request.Request.CompanyId,
                request.Request.Name);

            throw;
        }
    }
}