using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;

namespace Persistence.Repositories;

public class StockRequestLineRepository : IStockRequestLineRepository
{
    private readonly AppDbContext _context;

    public StockRequestLineRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Remove(StockRequestLine entity)
    {
        _context.StockRequestLines.Remove(entity);
    }
}