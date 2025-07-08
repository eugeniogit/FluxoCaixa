namespace FluxoCaixa.Consolidado.Shared.Contracts.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string destination);
}