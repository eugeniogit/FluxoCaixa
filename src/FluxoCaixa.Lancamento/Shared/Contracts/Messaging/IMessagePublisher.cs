namespace FluxoCaixa.Lancamento.Shared.Contracts.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string destination);
}