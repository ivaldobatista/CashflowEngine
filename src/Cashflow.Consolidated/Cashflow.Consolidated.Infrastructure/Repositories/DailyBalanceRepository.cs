using Cashflow.Consolidated.Application.Ports;
using Cashflow.Consolidated.Domain;
using Cashflow.Consolidated.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cashflow.Consolidated.Infrastructure.Repositories;

public class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly ConsolidatedDbContext _context;
    private readonly ILogger<DailyBalanceRepository> _logger;


    public DailyBalanceRepository(ConsolidatedDbContext context, ILogger<DailyBalanceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(DailyBalance dailyBalance)
    {
        await _context.DailyBalances.AddAsync(dailyBalance);
        await _context.SaveChangesAsync();
    }

    public async Task<DailyBalance?> GetByDateAsync(DateTime date)
    {
        var searchDate = date.Date;
        _logger.LogInformation("Iniciando busca no repositório por saldo na data: {SearchDate}", searchDate.ToShortDateString());

        var balance = await _context.DailyBalances.FirstOrDefaultAsync(b => b.Date == searchDate);

        if (balance != null)
        {
            _logger.LogInformation("Registro encontrado para a data {SearchDate}. Saldo: {Balance}", searchDate.ToShortDateString(), balance.Balance);
        }
        else
        {
            _logger.LogWarning("Nenhum registro de saldo encontrado para a data solicitada: {SearchDate}", searchDate.ToShortDateString());

            var existingDates = await _context.DailyBalances.Select(b => b.Date).ToListAsync();
            if (existingDates.Any())
            {
                var datesString = string.Join(", ", existingDates.Select(d => d.ToShortDateString()));
                _logger.LogInformation("Datas com saldos disponíveis no banco: [ {ExistingDates} ]", datesString);
            }
            else
            {
                _logger.LogInformation("Não há nenhum registro de saldo no banco de dados.");
            }
        }

        return balance;
    }

    public async Task UpdateAsync(DailyBalance dailyBalance)
    {
        _context.DailyBalances.Update(dailyBalance);
        await _context.SaveChangesAsync();
    }
}