using Application.Abstractions.Repositories;
using Application.Common.Interfaces.Abstracts.Repositories;

namespace Application.Common.Helpers;

/// <summary>
/// Decides which department a position or an employee belongs to when the company does not use departments
/// (the SuperAdmin switched the module off): nothing is picked, so the company's own default department stands in.
/// With the module on, the department must be chosen as before - a missing one stays 0 and the handler refuses it.
/// </summary>
public class DepartmentResolver
{
    /// <summary>Name of the department that holds everything while the module is off.</summary>
    public const string DefaultDepartmentName = "Ümumi";

    public const string ModuleOffMessage = "Departamentlər modulu bu şirkətdə söndürülüb.";

    private readonly ICompanySettingsRepository _settings;
    private readonly IDepartmentRepository _departments;
    private readonly IPositionRepository _positions;

    public DepartmentResolver(ICompanySettingsRepository settings, IDepartmentRepository departments, IPositionRepository positions)
    {
        _settings = settings;
        _departments = departments;
        _positions = positions;
    }

    public async Task<bool> IsModuleOffAsync(int companyId, CancellationToken ct)
        => companyId > 0 && (await _settings.GetByCompanyIdAsync(companyId, ct))?.ModuleDepartment == false;

    /// <summary>A new position: the chosen department, else (module off) the company's default one.</summary>
    public async Task<int> ForNewPositionAsync(int companyId, int requestedDepartmentId, CancellationToken ct)
    {
        if (requestedDepartmentId > 0) return requestedDepartmentId;
        if (!await IsModuleOffAsync(companyId, ct)) return 0;
        return (await _departments.GetOrCreateByNameAsync(companyId, DefaultDepartmentName, ct)).Id;
    }

    /// <summary>An edited position: the chosen department, else (module off) the one it already has.</summary>
    public async Task<int> ForPositionUpdateAsync(int companyId, int requestedDepartmentId, int currentDepartmentId, CancellationToken ct)
    {
        if (requestedDepartmentId > 0) return requestedDepartmentId;
        return await IsModuleOffAsync(companyId, ct) ? currentDepartmentId : 0;
    }

    /// <summary>An employee: the chosen department, else (module off) the department of the chosen position.</summary>
    public async Task<int> ForEmployeeAsync(int companyId, int requestedDepartmentId, int positionId, CancellationToken ct)
    {
        if (requestedDepartmentId > 0) return requestedDepartmentId;
        if (!await IsModuleOffAsync(companyId, ct)) return 0;
        return (await _positions.GetByIdAsync(positionId, companyId, ct))?.DepartmentId ?? 0;
    }
}
