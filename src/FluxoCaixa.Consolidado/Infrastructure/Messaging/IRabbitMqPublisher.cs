namespace FluxoCaixa.Consolidado.Infrastructure.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishMarcarConsolidadosEventAsync(MarcarConsolidadosEvent marcarConsolidadosEvent);
}