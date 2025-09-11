using Cashflow.Launch.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Cashflow.Launch.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessageBrokerPublisher, IDisposable
{
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "transactions_exchange";

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMq:HostName"],
                UserName = configuration["RabbitMq:UserName"],
                Password = configuration["RabbitMq:Password"],
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _logger.LogInformation("Conexão com RabbitMQ estabelecida.");

            _channel.ExchangeDeclare(
                        exchange: ExchangeName,
                        type: ExchangeType.Fanout,
                        durable: true,     
                        autoDelete: false,
                        arguments: null
                    );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível conectar ao RabbitMQ.");
            throw;
        }
    }

    public Task PublishAsync<T>(T message)
    {
        var jsonMessage = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(jsonMessage);

        _channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: "",
            basicProperties: null,
            body: body);

        _logger.LogInformation("Mensagem publicada na exchange '{ExchangeName}'.", ExchangeName);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}