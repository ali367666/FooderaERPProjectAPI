using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _context;

    public CompanyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Company>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Companies
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> AnyAsync(Expression<Func<Company, bool>> predicate, CancellationToken cancellationToken)
    {
        return await _context.Companies.AnyAsync(predicate, cancellationToken);
    }

    public async Task AddAsync(Company company, CancellationToken cancellationToken)
    {
        await _context.Companies.AddAsync(company, cancellationToken);
    }

    public void Update(Company company)
    {
        _context.Companies.Update(company);
    }

    public void Delete(Company company)
    {
        _context.Companies.Remove(company);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    
    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Companies
        .AnyAsync(x => x.Id == id, cancellationToken);
    }
}