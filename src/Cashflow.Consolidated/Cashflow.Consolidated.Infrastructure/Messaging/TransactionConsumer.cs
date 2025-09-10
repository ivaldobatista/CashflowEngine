using Cashflow.Consolidated.Application.DTOs;
using Cashflow.Consolidated.Application.Ports;
using Cashflow.Consolidated.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Cashflow.Consolidated.Infrastructure.Messaging;

public class TransactionConsumer : BackgroundService
{
    private readonly ILogger<TransactionConsumer> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "transactions_exchange";
    private const string QueueName = "consolidated_transactions_queue";

    public TransactionConsumer(IConfiguration configuration, ILogger<TransactionConsumer> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMq:HostName"],
                UserName = configuration["RabbitMq:UserName"],
                Password = configuration["RabbitMq:Password"],
                DispatchConsumersAsync = true
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _logger.LogInformation("Consumidor RabbitMQ conectado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível conectar o consumidor ao RabbitMQ.");
            throw;
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Fanout);
        _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: "");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var transactionEvent = JsonSerializer.Deserialize<TransactionCreatedEvent>(message);

                if (transactionEvent != null)
                {
                    _logger.LogInformation("Processando evento de transação {TransactionId}.", transactionEvent.TransactionId);
                    await ProcessMessage(transactionEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do RabbitMQ.");
            }
            finally
            {
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Consumidor RabbitMQ iniciado e aguardando mensagens.");

        return Task.CompletedTask;
    }

    private async Task ProcessMessage(TransactionCreatedEvent transactionEvent)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDailyBalanceRepository>();

        var transactionDate = transactionEvent.TimestampUtc.Date;
        var dailyBalance = await repository.GetByDateAsync(transactionDate);

        bool isNew = dailyBalance == null;
        dailyBalance ??= new DailyBalance(transactionDate);

        if (transactionEvent.Type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
        {
            dailyBalance.AddCredit(transactionEvent.Amount);
        }
        else if (transactionEvent.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
        {
            dailyBalance.ApplyDebit(transactionEvent.Amount);
        }

        if (isNew)
        {
            await repository.AddAsync(dailyBalance);
        }
        else
        {
            await repository.UpdateAsync(dailyBalance);
        }
        _logger.LogInformation("Saldo para o dia {Date} atualizado para {Balance:C}", dailyBalance.Date.ToShortDateString(), dailyBalance.Balance);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}