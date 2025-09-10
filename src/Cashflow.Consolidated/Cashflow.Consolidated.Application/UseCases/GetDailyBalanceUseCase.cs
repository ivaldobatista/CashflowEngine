using Cashflow.Consolidated.Application.Ports;
using Cashflow.Consolidated.Domain;

namespace Cashflow.Consolidated.Application.UseCases;

public class GetDailyBalanceUseCase
{
    private readonly IDailyBalanceRepository _repository;

    public GetDailyBalanceUseCase(IDailyBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<DailyBalance> ExecuteAsync(DateTime date)
    {
        var requestedDate = date.Date;
        var dailyBalance = await _repository.GetByDateAsync(requestedDate);

        return dailyBalance ?? new DailyBalance(requestedDate);
    }
}