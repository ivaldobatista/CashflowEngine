namespace Cashflow.Consolidated.Domain;

public class DailyBalance
{
    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime LastUpdateUtc { get; private set; }

    public DailyBalance() { }

    public DailyBalance(DateTime date)
    {
        Id = Guid.NewGuid();
        Date = date.Date;
        Balance = 0;
        LastUpdateUtc = DateTime.UtcNow;
    }

    public void AddCredit(decimal amount)
    {
        if (amount <= 0) return;
        Balance += amount;
        LastUpdateUtc = DateTime.UtcNow;
    }

    public void ApplyDebit(decimal amount)
    {
        if (amount <= 0) return;
        Balance -= amount;
        LastUpdateUtc = DateTime.UtcNow;
    }
}