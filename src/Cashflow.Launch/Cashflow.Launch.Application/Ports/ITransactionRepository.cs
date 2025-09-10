using Cashflow.Launch.Domain;

namespace Cashflow.Launch.Application.Ports;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction);
}