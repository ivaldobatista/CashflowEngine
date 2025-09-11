using Cashflow.Consolidated.Application.DTOs;
using Cashflow.Consolidated.Application.Ports;
using Cashflow.Consolidated.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;

namespace Cashflow.Consolidated.Infrastructure.Messaging;

public class TransactionConsumer : BackgroundService
{
    private readonly ILogger<TransactionConsumer> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "transactions_exchange";
    private const string QueueName = "consolidated_transactions_queue";

    public TransactionConsumer(IConfiguration configuration,
        ILogger<TransactionConsumer> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _scopeFactory = serviceScopeFactory;
        _configuration = configuration;

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        _logger.LogInformation("TransactionConsumer iniciado. Preparando conexão com RabbitMQ...");
        await GarantirConexaoComRetriesAsync(stoppingToken);

        stoppingToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Declarando topologia de mensageria (exchange/queue/bind)...");
        _channel!.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Fanout, durable: true, autoDelete: false);
        _channel!.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: "");

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

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

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // ignore – desligamento solicitado
        }
    }

    private async Task ProcessMessage(TransactionCreatedEvent transactionEvent)
    {
        using var scope = _scopeFactory.CreateScope();
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

    private async Task GarantirConexaoComRetriesAsync(CancellationToken ct)
    {
        var host = _configuration["RabbitMq:HostName"] ?? "rabbitmq";
        var user = _configuration["RabbitMq:UserName"] ?? "guest";
        var pass = _configuration["RabbitMq:Password"] ?? "guest";
        var port = int.TryParse(_configuration["RabbitMq:Port"], out var p) ? p : AmqpTcpEndpoint.UseDefaultPort;

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = user,
            Password = pass,
            Port = port,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
            ClientProvidedName = "cashflow-consolidated-consumer"
        };

        var tentativa = 0;
        var delay = TimeSpan.FromSeconds(2);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                tentativa++;
                _logger.LogWarning("Conectando ao RabbitMQ {Host}:{Port} (tentativa {Tentativa})...", host, port, tentativa);
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _logger.LogInformation("Conexão com RabbitMQ estabelecida.");
                return;
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogError(ex, "Não foi possível conectar ao RabbitMQ. Nova tentativa em {Delay}s...", delay.TotalSeconds);
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (TaskCanceledException) when (ct.IsCancellationRequested)
                {
                    break; // encerrando
                }

                if (delay < TimeSpan.FromSeconds(30))
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // backoff exponencial (cap 30s)
            }
        }

        ct.ThrowIfCancellationRequested();
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}