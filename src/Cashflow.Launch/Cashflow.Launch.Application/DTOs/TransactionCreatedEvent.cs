namespace Cashflow.Launch.Application.DTOs;

public record TransactionCreatedEvent(Guid TransactionId, decimal Amount, string Type, DateTime TimestampUtc);