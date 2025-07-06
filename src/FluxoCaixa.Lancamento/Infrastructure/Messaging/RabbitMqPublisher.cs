using FluxoCaixa.Lancamento.Configuration;
using FluxoCaixa.Lancamento.Domain;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Lancamento.Infrastructure.Messaging;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private IConnection? _connection;
    private IModel? _channel;

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

    public async Task PublishLancamentoEventAsync(LancamentoEvent lancamentoEvent)
    {
        await _retryPolicy.ExecuteAsync(() =>
        {
            EnsureConnection();
            
            var message = JsonSerializer.Serialize(lancamentoEvent);
            var body = Encoding.UTF8.GetBytes(message);

            _channel!.BasicPublish(
                exchange: "",
                routingKey: _settings.QueueName,
                basicProperties: null,
                body: body);

            _logger.LogInformation("Evento de lançamento publicado: {LancamentoId}", lancamentoEvent.Id);
            
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

            _channel.QueueDeclare(
                queue: _settings.QueueName,
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