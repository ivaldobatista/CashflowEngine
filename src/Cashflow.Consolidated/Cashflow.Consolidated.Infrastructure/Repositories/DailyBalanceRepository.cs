using Cashflow.Consolidated.Application.Ports;
using Cashflow.Consolidated.Domain;
using Cashflow.Consolidated.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cashflow.Consolidated.Infrastructure.Repositories;

public class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly ConsolidatedDbContext _context;

    public DailyBalanceRepository(ConsolidatedDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DailyBalance dailyBalance)
    {
        await _context.DailyBalances.AddAsync(dailyBalance);
        await _context.SaveChangesAsync();
    }

    public async Task<DailyBalance?> GetByDateAsync(DateTime date)
    {
        return await _context.DailyBalances.FirstOrDefaultAsync(b => b.Date == date.Date);
    }

    public async Task UpdateAsync(DailyBalance dailyBalance)
    {
        _context.DailyBalances.Update(dailyBalance);
        await _context.SaveChangesAsync();
    }
}