using Cashflow.Launch.Application.DTOs;
using Cashflow.Launch.Application.Ports;
using Cashflow.Launch.Domain;
using Microsoft.Extensions.Logging;

namespace Cashflow.Launch.Application.UseCases;

public class CreateTransactionUseCase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMessageBrokerPublisher _messageBrokerPublisher;
    private readonly ILogger<CreateTransactionUseCase> _logger;

    public CreateTransactionUseCase(
        ITransactionRepository transactionRepository,
        IMessageBrokerPublisher messageBrokerPublisher,
        ILogger<CreateTransactionUseCase> logger)
    {
        _transactionRepository = transactionRepository;
        _messageBrokerPublisher = messageBrokerPublisher;
        _logger = logger;
    }

    public async Task ExecuteAsync(CreateTransactionRequest request)
    {
        if (!Enum.TryParse<TransactionType>(request.Type, true, out var transactionType))
        {
            throw new ArgumentException("Invalid transaction type.", nameof(request.Type));
        }

        try
        {
            var transaction = new Transaction(request.Amount, transactionType);

            _logger.LogInformation("Nova transação criada com ID: {TransactionId}", transaction.Id);

            await _transactionRepository.AddAsync(transaction);

            var transactionEvent = new TransactionCreatedEvent(
                transaction.Id,
                transaction.Amount,
                transaction.Type.ToString(),
                transaction.TimestampUtc);

            await _messageBrokerPublisher.PublishAsync(transactionEvent);

            _logger.LogInformation("Evento da transação {TransactionId} publicado com sucesso.", transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao processar a transação.");
            throw;
        }
    }
}