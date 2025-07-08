namespace FluxoCaixa.Consolidado.Shared.Infrastructure.Messaging;

public class LancamentoBackgroundService : BackgroundService
{
    private readonly LancamentoConsumer _consumer;
    private readonly ILogger<LancamentoBackgroundService> _logger;

    public LancamentoBackgroundService(LancamentoConsumer consumer, ILogger<LancamentoBackgroundService> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Background Service iniciado");

        try
        {
            await _consumer.StartConsumingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no RabbitMQ Background Service");
        }
        finally
        {
            await _consumer.StopConsumingAsync();
            _logger.LogInformation("RabbitMQ Background Service parado");
        }
    }
}