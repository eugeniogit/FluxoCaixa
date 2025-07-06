using FluxoCaixa.Lancamento.Configuration;
using FluxoCaixa.Lancamento.Features.MarcarConsolidados;
using MediatR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Lancamento.Infrastructure.Messaging;

public class MarcarConsolidadosEvent
{
    public List<string> LancamentoIds { get; set; } = new();
    public DateTime DataProcessamento { get; set; } = DateTime.UtcNow;
}

public class MarcarConsolidadosConsumer : IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarcarConsolidadosConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private EventingBasicConsumer? _consumer;
    private const string MarcarConsolidadosQueueName = "marcar_consolidados_events";

    public MarcarConsolidadosConsumer(
        IOptions<RabbitMqSettings> options,
        IServiceProvider serviceProvider,
        ILogger<MarcarConsolidadosConsumer> logger)
    {
        _settings = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ConnectAsync();
            
            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                try
                {
                    var marcarConsolidadosEvent = JsonSerializer.Deserialize<MarcarConsolidadosEvent>(message);
                    if (marcarConsolidadosEvent != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        
                        var command = new MarcarConsolidadosCommand
                        {
                            LancamentoIds = marcarConsolidadosEvent.LancamentoIds
                        };
                        
                        await mediator.Send(command, cancellationToken);
                        
                        _channel!.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Evento MarcarConsolidados processado: {Count} lançamentos", marcarConsolidadosEvent.LancamentoIds.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar evento MarcarConsolidados: {Message}", message);
                    _channel!.BasicReject(ea.DeliveryTag, false);
                }
            };

            _channel.BasicConsume(
                queue: MarcarConsolidadosQueueName,
                autoAck: false,
                consumer: _consumer);

            _logger.LogInformation("Iniciado consumo de eventos MarcarConsolidados do RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar consumo de eventos MarcarConsolidados do RabbitMQ");
            throw;
        }
    }

    public Task StopConsumingAsync()
    {
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

        _channel.QueueDeclare(
            queue: MarcarConsolidadosQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _logger.LogInformation("Conectado ao RabbitMQ para MarcarConsolidados");
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _consumer = null;
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}