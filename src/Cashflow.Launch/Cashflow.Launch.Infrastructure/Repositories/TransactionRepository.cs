using Cashflow.Launch.Application.Ports;
using Cashflow.Launch.Domain;
using Cashflow.Launch.Infrastructure.Persistence;

namespace Cashflow.Launch.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly LaunchDbContext _context;

    public TransactionRepository(LaunchDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }
}