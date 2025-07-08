using Microsoft.Extensions.Options;
using FluxoCaixa.Lancamento.Shared.Contracts.Messaging;
using FluxoCaixa.Lancamento.Shared.Configurations;

namespace FluxoCaixa.Lancamento.Shared.Infrastructure.Messaging;

public class MessageBrokerFactory : IMessageBrokerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<MessageBrokerFactory> _logger;

    public MessageBrokerFactory(
        IServiceProvider serviceProvider,
        IOptions<RabbitMqSettings> settings,
        ILogger<MessageBrokerFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    public IMessagePublisher CreatePublisher()
    {
        return _serviceProvider.GetRequiredService<LancamentoPublisher>();
    }

    public IMessageConsumer CreateConsumer()
    {
        return _serviceProvider.GetRequiredService<LancamentosConsolidadosConsumer>();
    }
}