namespace FluxoCaixa.Lancamento.Infrastructure.Messaging.Abstractions;

public interface IMessageConsumer
{
    Task StartConsumingAsync<T>(string source, Func<T, Task> messageHandler, CancellationToken cancellationToken = default);
    Task StopConsumingAsync();
}