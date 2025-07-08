using FluxoCaixa.Lancamento.Features.ConsolidarLancamentos;
using FluxoCaixa.Lancamento.Shared.Configurations;
using FluxoCaixa.Lancamento.Shared.Contracts.Messaging;
using FluxoCaixa.Lancamento.Shared.Domain.Events;
using MediatR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Lancamento.Shared.Infrastructure.Messaging;

public class LancamentosConsolidadosConsumer : IMessageConsumer, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LancamentosConsolidadosConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private EventingBasicConsumer? _consumer;
    private bool _disposed = false;
    private const string MarcarConsolidadosQueueName = "marcar_consolidados_events";

    public LancamentosConsolidadosConsumer(
        IOptions<RabbitMqSettings> options,
        IServiceProvider serviceProvider,
        ILogger<LancamentosConsolidadosConsumer> logger)
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
        await StartConsumingAsync<LancamentosConsolidadosEvent>(MarcarConsolidadosQueueName, async (lancamentosConsolidadosEvent) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            var command = new ConsolidarLancamentosCommand
            {
                LancamentoIds = lancamentosConsolidadosEvent.LancamentoIds
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
            _logger.LogInformation("Consumo de eventos MarcarConsolidados do RabbitMQ interrompido");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parar consumo de eventos MarcarConsolidados do RabbitMQ");
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

        DeclareQueue(MarcarConsolidadosQueueName);

        _logger.LogInformation("Conectado ao RabbitMQ para MarcarConsolidados");
        
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