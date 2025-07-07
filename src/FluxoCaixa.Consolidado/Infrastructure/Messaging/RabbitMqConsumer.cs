using FluxoCaixa.Consolidado.Configuration;
using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Features.ConsolidarLancamento;
using MediatR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Consolidado.Infrastructure.Messaging;

public class RabbitMqConsumer : IRabbitMqConsumer, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private EventingBasicConsumer? _consumer;

    public RabbitMqConsumer(
        IOptions<RabbitMqSettings> options,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqConsumer> logger)
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
                    var lancamentoEvent = JsonSerializer.Deserialize<LancamentoEvent>(message);
                    if (lancamentoEvent != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        
                        var command = new ConsolidarLancamentoCommand
                        {
                            LancamentoEvent = lancamentoEvent
                        };
                        
                        await mediator.Send(command, cancellationToken);
                        
                        _channel!.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Evento de lançamento processado: {LancamentoId}", lancamentoEvent.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar evento de lançamento: {Message}", message);
                    _channel!.BasicReject(ea.DeliveryTag, false);
                }
            };

            _channel.BasicConsume(
                queue: _settings.QueueName,
                autoAck: false,
                consumer: _consumer);

            _logger.LogInformation("Iniciado consumo de mensagens do RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar consumo de mensagens do RabbitMQ");
            throw;
        }
    }

    public Task StopConsumingAsync()
    {
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

        _channel.QueueDeclare(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _logger.LogInformation("Conectado ao RabbitMQ");
        
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