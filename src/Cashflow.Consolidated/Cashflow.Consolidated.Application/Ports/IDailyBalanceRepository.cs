using Cashflow.Consolidated.Domain;

namespace Cashflow.Consolidated.Application.Ports;

public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetByDateAsync(DateTime date);
    Task UpdateAsync(DailyBalance dailyBalance);
    Task AddAsync(DailyBalance dailyBalance);
}