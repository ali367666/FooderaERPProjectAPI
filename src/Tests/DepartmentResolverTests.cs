using Application.Abstractions.Repositories;
using Application.Common.Helpers;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using NSubstitute;
using Xunit;

namespace Tests;

/// <summary>A company without the Departments module still creates positions and employees: its default department stands in.</summary>
public class DepartmentResolverTests
{
    private const int Company = 1;

    private readonly ICompanySettingsRepository _settings = Substitute.For<ICompanySettingsRepository>();
    private readonly IDepartmentRepository _departments = Substitute.For<IDepartmentRepository>();
    private readonly IPositionRepository _positions = Substitute.For<IPositionRepository>();
    private readonly DepartmentResolver _resolver;

    public DepartmentResolverTests()
    {
        _resolver = new DepartmentResolver(_settings, _departments, _positions);
        _departments.GetOrCreateByNameAsync(Company, DepartmentResolver.DefaultDepartmentName, Arg.Any<CancellationToken>())
            .Returns(new Department { Id = 77, CompanyId = Company, Name = DepartmentResolver.DefaultDepartmentName });
    }

    private void ModuleIs(bool on)
        => _settings.GetByCompanyIdAsync(Company, Arg.Any<CancellationToken>()).Returns(new CompanySettings { CompanyId = Company, ModuleDepartment = on });

    [Fact]
    public async Task A_chosen_department_is_always_kept()
    {
        ModuleIs(false);

        Assert.Equal(5, await _resolver.ForNewPositionAsync(Company, 5, default));
        Assert.Equal(5, await _resolver.ForEmployeeAsync(Company, 5, 9, default));
        Assert.Equal(5, await _resolver.ForPositionUpdateAsync(Company, 5, 3, default));
    }

    [Fact]
    public async Task With_the_module_on_a_missing_department_stays_missing()
    {
        ModuleIs(true);

        Assert.Equal(0, await _resolver.ForNewPositionAsync(Company, 0, default));
        Assert.Equal(0, await _resolver.ForEmployeeAsync(Company, 0, 9, default));
        Assert.Equal(0, await _resolver.ForPositionUpdateAsync(Company, 0, 3, default));
        await _departments.DidNotReceive().GetOrCreateByNameAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_company_without_settings_keeps_departments_on()
    {
        _settings.GetByCompanyIdAsync(Company, Arg.Any<CancellationToken>()).Returns((CompanySettings?)null);

        Assert.Equal(0, await _resolver.ForNewPositionAsync(Company, 0, default));
    }

    [Fact]
    public async Task With_the_module_off_a_new_position_goes_to_the_default_department()
    {
        ModuleIs(false);

        Assert.Equal(77, await _resolver.ForNewPositionAsync(Company, 0, default));
    }

    [Fact]
    public async Task With_the_module_off_an_employee_takes_the_department_of_its_position()
    {
        ModuleIs(false);
        _positions.GetByIdAsync(9, Company, Arg.Any<CancellationToken>()).Returns(new Position { Id = 9, CompanyId = Company, DepartmentId = 42 });

        Assert.Equal(42, await _resolver.ForEmployeeAsync(Company, 0, 9, default));
    }

    [Fact]
    public async Task With_the_module_off_an_employee_of_an_unknown_position_gets_nothing()
    {
        ModuleIs(false);
        _positions.GetByIdAsync(9, Company, Arg.Any<CancellationToken>()).Returns((Position?)null);

        Assert.Equal(0, await _resolver.ForEmployeeAsync(Company, 0, 9, default));
    }

    [Fact]
    public async Task With_the_module_off_an_edited_position_keeps_its_department()
    {
        ModuleIs(false);

        Assert.Equal(3, await _resolver.ForPositionUpdateAsync(Company, 0, 3, default));
    }
}
