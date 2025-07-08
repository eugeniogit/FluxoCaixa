namespace FluxoCaixa.Consolidado.Shared.Contracts.Messaging;

public interface IMessageConsumer
{
    Task StartConsumingAsync<T>(string source, Func<T, Task> messageHandler, CancellationToken cancellationToken = default);
    Task StopConsumingAsync();
}