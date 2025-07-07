namespace FluxoCaixa.Consolidado.Infrastructure.Messaging.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string destination);
}