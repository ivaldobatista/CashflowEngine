namespace Cashflow.Consolidated.Application.DTOs;

public record TransactionCreatedEvent(Guid TransactionId, decimal Amount, string Type, DateTime TimestampUtc);