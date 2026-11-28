using Application.BackOffice;
using Application.Common.Exceptions;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class EmployeeLeaveService : IEmployeeLeaveService
{
    private readonly AppDbContext _db;

    public EmployeeLeaveService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<EmployeeLeaveResponse>> GetAsync(int companyId, int? employeeId, CancellationToken ct)
    {
        var rows = await _db.EmployeeLeaves.AsNoTracking()
            .Where(l => l.CompanyId == companyId && (employeeId == null || l.EmployeeId == employeeId))
            .OrderByDescending(l => l.FromDate)
            .Select(l => new { l.Id, l.EmployeeId, l.Employee.FirstName, l.Employee.LastName, l.FromDate, l.ToDate, l.Note })
            .ToListAsync(ct);
        return rows
            .Select(l => new EmployeeLeaveResponse(l.Id, l.EmployeeId, $"{l.FirstName} {l.LastName}".Trim(), l.FromDate, l.ToDate, l.Note))
            .ToList();
    }

    public async Task<EmployeeLeaveResponse> AddAsync(int companyId, SaveEmployeeLeaveRequest request, CancellationToken ct)
    {
        var from = request.FromDate.Date;
        var to = request.ToDate.Date;
        if (from == default || to < from)
            throw new BadRequestException("Məzuniyyətin tarixləri yanlışdır.");
        var note = request.Note?.Trim();
        if (note?.Length > 300)
            throw new BadRequestException("Qeyd 300 simvoldan çox ola bilməz.");

        var employee = await _db.Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.CompanyId == companyId, ct)
            ?? throw new NotFoundException("İşçi tapılmadı.");
        if (await _db.EmployeeLeaves.AnyAsync(l => l.EmployeeId == employee.Id && l.FromDate <= to && l.ToDate >= from, ct))
            throw new BadRequestException("Bu tarixlərdə işçinin artıq məzuniyyəti var.");

        var leave = new EmployeeLeave
        {
            CompanyId = companyId,
            EmployeeId = employee.Id,
            FromDate = from,
            ToDate = to,
            Note = string.IsNullOrEmpty(note) ? null : note
        };
        _db.EmployeeLeaves.Add(leave);
        await _db.SaveChangesAsync(ct);
        return new EmployeeLeaveResponse(
            leave.Id, employee.Id, $"{employee.FirstName} {employee.LastName}".Trim(), from, to, leave.Note);
    }

    public async Task DeleteAsync(int companyId, int id, CancellationToken ct)
    {
        var leave = await _db.EmployeeLeaves.FirstOrDefaultAsync(l => l.Id == id && l.CompanyId == companyId, ct)
            ?? throw new NotFoundException("Məzuniyyət tapılmadı.");
        _db.EmployeeLeaves.Remove(leave);
        await _db.SaveChangesAsync(ct);
    }
}
