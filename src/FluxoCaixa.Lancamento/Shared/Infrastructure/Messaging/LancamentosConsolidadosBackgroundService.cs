namespace FluxoCaixa.Lancamento.Shared.Infrastructure.Messaging;

public class LancamentosConsolidadosBackgroundService : BackgroundService
{
    private readonly LancamentosConsolidadosConsumer _consumer;
    private readonly ILogger<LancamentosConsolidadosBackgroundService> _logger;

    public LancamentosConsolidadosBackgroundService(
        LancamentosConsolidadosConsumer consumer,
        ILogger<LancamentosConsolidadosBackgroundService> logger)
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