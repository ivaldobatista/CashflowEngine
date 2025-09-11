namespace Cashflow.Launch.Domain;

public class Transaction
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public string? Description { get; private set; }
    public DateTime TimestampUtc { get; private set; }

    private Transaction() { }

    public Transaction(decimal amount, TransactionType type, string? description)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Transaction amount must be a positive value.", nameof(amount));
        }

        Id = Guid.NewGuid();
        Amount = amount;
        Type = type;
        Description = description;
        TimestampUtc = DateTime.UtcNow;
    }
}