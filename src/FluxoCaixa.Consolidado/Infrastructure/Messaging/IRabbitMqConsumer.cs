namespace FluxoCaixa.Consolidado.Infrastructure.Messaging;

public interface IRabbitMqConsumer
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
    Task StopConsumingAsync();
}