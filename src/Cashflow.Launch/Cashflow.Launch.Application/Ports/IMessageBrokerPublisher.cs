namespace Cashflow.Launch.Application.Ports;

public interface IMessageBrokerPublisher
{
    Task PublishAsync<T>(T message);
}