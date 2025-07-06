namespace FluxoCaixa.Lancamento.Infrastructure.Messaging;

public class MarcarConsolidadosBackgroundService : BackgroundService
{
    private readonly MarcarConsolidadosConsumer _consumer;
    private readonly ILogger<MarcarConsolidadosBackgroundService> _logger;

    public MarcarConsolidadosBackgroundService(
        MarcarConsolidadosConsumer consumer,
        ILogger<MarcarConsolidadosBackgroundService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("MarcarConsolidados Background Service iniciado");
            await _consumer.StartConsumingAsync(stoppingToken);
            
            // Manter o serviço executando até o cancelamento
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MarcarConsolidados Background Service cancelado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no MarcarConsolidados Background Service");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parando MarcarConsolidados Background Service");
        await _consumer.StopConsumingAsync();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}