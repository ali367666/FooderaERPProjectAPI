using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<User?> GetByCompanyAndCodeAsync(int companyId, string code, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code, cancellationToken);
    }

    public async Task<User?> GetByRfidCardIdAsync(string rfidCardId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.RfidCardId == rfidCardId, cancellationToken);
    }

    public async Task<bool> HasRotatingPinRoleAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.RequiresRotatingPin)
            .AnyAsync(requiresRotatingPin => requiresRotatingPin, cancellationToken);
    }

    public async Task<List<User>> GetAllByWarehouseIdAsync(int warehouseId, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(x => x.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
    }

    public void Update(User user) => _context.Users.Update(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}