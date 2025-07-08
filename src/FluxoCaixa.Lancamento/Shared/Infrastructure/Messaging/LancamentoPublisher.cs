using FluxoCaixa.Lancamento.Shared.Configurations;
using FluxoCaixa.Lancamento.Shared.Contracts.Messaging;
using FluxoCaixa.Lancamento.Shared.Domain.Events;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Lancamento.Shared.Infrastructure.Messaging;

public class LancamentoPublisher : IMessagePublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<LancamentoPublisher> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed = false;

    public LancamentoPublisher(IOptions<RabbitMqSettings> options, ILogger<LancamentoPublisher> logger)
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

    public async Task PublishAsync<T>(T message, string destination)
    {
        if (_disposed)
        {
            _logger.LogWarning("Tentativa de publicar mensagem em publisher descartado");
            return;
        }

        await _retryPolicy.ExecuteAsync(() =>
        {
            if (_disposed) return Task.CompletedTask;
            
            EnsureConnection();
            
            var messageJson = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageJson);

            _channel!.BasicPublish(
                exchange: "",
                routingKey: destination,
                basicProperties: null,
                body: body);

            _logger.LogInformation("Mensagem publicada para {Destination}: {MessageType}", destination, typeof(T).Name);
            
            return Task.CompletedTask;
        });
    }

    public async Task PublishLancamentoEventAsync(LancamentoEvent lancamentoEvent)
    {
        await PublishAsync(lancamentoEvent, _settings.QueueName);
    }

    private void EnsureConnection()
    {
        if (_disposed || _connection?.IsOpen == true && _channel?.IsOpen == true)
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

            DeclareQueue(_settings.QueueName);

            _logger.LogInformation("Conexão com RabbitMQ estabelecida");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar com RabbitMQ");
            throw;
        }
    }

    private void DeclareQueue(string queueName)
    {
        _channel?.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao fechar conexão RabbitMQ durante dispose");
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}