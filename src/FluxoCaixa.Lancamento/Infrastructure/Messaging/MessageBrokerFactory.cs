using FluxoCaixa.Lancamento.Infrastructure.Messaging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace FluxoCaixa.Lancamento.Infrastructure.Messaging;

public class MessageBrokerFactory : IMessageBrokerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MessageBrokerSettings _settings;
    private readonly ILogger<MessageBrokerFactory> _logger;

    public MessageBrokerFactory(
        IServiceProvider serviceProvider,
        IOptions<MessageBrokerSettings> settings,
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