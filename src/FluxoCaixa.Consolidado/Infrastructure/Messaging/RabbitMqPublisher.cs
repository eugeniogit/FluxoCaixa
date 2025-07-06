using FluxoCaixa.Consolidado.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Consolidado.Infrastructure.Messaging;

public class MarcarConsolidadosEvent
{
    public List<string> LancamentoIds { get; set; } = new();
    public DateTime DataProcessamento { get; set; } = DateTime.UtcNow;
}

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private IConnection? _connection;
    private IModel? _channel;
    private const string MarcarConsolidadosQueueName = "marcar_consolidados_events";

    public RabbitMqPublisher(IOptions<RabbitMqSettings> options, ILogger<RabbitMqPublisher> logger)
    {
        _settings = options.Value;
        _logger = logger;
        
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Tentativa {RetryCount} de publicação no RabbitMQ falhou. Tentando novamente em {Delay}s", 
                        retryCount, timespan.TotalSeconds);
                });
    }

    public async Task PublishMarcarConsolidadosEventAsync(MarcarConsolidadosEvent marcarConsolidadosEvent)
    {
        await _retryPolicy.ExecuteAsync(() =>
        {
            EnsureConnection();
            
            var message = JsonSerializer.Serialize(marcarConsolidadosEvent);
            var body = Encoding.UTF8.GetBytes(message);

            _channel!.BasicPublish(
                exchange: "",
                routingKey: MarcarConsolidadosQueueName,
                basicProperties: null,
                body: body);

            _logger.LogInformation("Evento MarcarConsolidados publicado: {Count} lançamentos", marcarConsolidadosEvent.LancamentoIds.Count);
            
            return Task.CompletedTask;
        });
    }

    private void EnsureConnection()
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            return;

        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar fila para marcar consolidados
            _channel.QueueDeclare(
                queue: MarcarConsolidadosQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("Conexão com RabbitMQ estabelecida");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}