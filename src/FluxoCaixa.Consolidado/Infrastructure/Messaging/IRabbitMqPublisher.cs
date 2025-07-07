namespace FluxoCaixa.Consolidado.Infrastructure.Messaging;

public interface IRabbitMqPublisher
{
    Task PublishLancamentoConsolidadoEventAsync(LancamentosConsolidadosEvent marcarConsolidadosEvent);
}