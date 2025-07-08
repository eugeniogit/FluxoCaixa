using FluxoCaixa.Consolidado.Features.ConsolidarLancamento;
using FluxoCaixa.Consolidado.Shared.Contracts.Messaging;
using FluxoCaixa.Consolidado.Shared.Domain.Events;
using MediatR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Consolidado.Shared.Infrastructure.Messaging;

public class LancamentoConsumer : IMessageConsumer, IDisposable
{
    private readonly MessageBrokerSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LancamentoConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private EventingBasicConsumer? _consumer;
    private bool _disposed = false;

    public LancamentoConsumer(
        IOptions<MessageBrokerSettings> options,
        IServiceProvider serviceProvider,
        ILogger<LancamentoConsumer> logger)
    {
        _settings = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartConsumingAsync<T>(string source, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            _logger.LogWarning("Tentativa de iniciar consumo em consumer descartado");
            return;
        }

        try
        {
            await ConnectAsync();
            
            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                
                try
                {
                    var message = JsonSerializer.Deserialize<T>(messageJson);
                    if (message != null)
                    {
                        await messageHandler(message);
                        _channel!.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Mensagem processada de {Source}: {MessageType}", source, typeof(T).Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem de {Source}: {Message}", source, messageJson);
                    _channel!.BasicReject(ea.DeliveryTag, false);
                }
            };

            _channel.BasicConsume(
                queue: source,
                autoAck: false,
                consumer: _consumer);

            _logger.LogInformation("Iniciado consumo de mensagens de {Source}", source);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar consumo de mensagens de {Source}", source);
            throw;
        }
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        await StartConsumingAsync<LancamentoEvent>(_settings.QueueName, async (lancamentoEvent) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            var command = new ConsolidarLancamentoCommand
            {
                LancamentoEvent = lancamentoEvent
            };
            
            await mediator.Send(command, cancellationToken);
        }, cancellationToken);
    }

    public Task StopConsumingAsync()
    {
        if (_disposed) return Task.CompletedTask;
        
        try
        {
            _channel?.Close();
            _connection?.Close();
            _logger.LogInformation("Consumo de mensagens do RabbitMQ interrompido");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parar consumo de mensagens do RabbitMQ");
        }
        
        return Task.CompletedTask;
    }

    private Task ConnectAsync()
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

        _logger.LogInformation("Conectado ao RabbitMQ");
        
        return Task.CompletedTask;
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
            _consumer = null;
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