namespace Cashflow.Launch.Domain.Tests;

public class TransactionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-150.75)]
    public void Constructor_Should_ThrowArgumentException_When_Amount_Is_Not_Positive(decimal invalidAmount)
    {
        var transactionType = TransactionType.Credit;

        Action act = () => new Transaction(invalidAmount, transactionType, "");

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Transaction amount must be a positive value", exception.Message);
    }

    [Fact]
    public void Constructor_Should_CreateInstance_When_Amount_Is_Positive()
    {
        var validAmount = 100.50m;
        var transactionType = TransactionType.Debit;

        var transaction = new Transaction(validAmount, transactionType, "");

        Assert.NotNull(transaction);
        Assert.Equal(validAmount, transaction.Amount);
        Assert.Equal(transactionType, transaction.Type);
        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.True(transaction.TimestampUtc > DateTime.UtcNow.AddMinutes(-1));
    }
}