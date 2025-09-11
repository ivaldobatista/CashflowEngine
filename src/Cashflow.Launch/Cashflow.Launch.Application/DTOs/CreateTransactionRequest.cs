namespace Cashflow.Launch.Application.DTOs;

public record CreateTransactionRequest(DateTime Date, decimal Amount, string Type, string? Description);