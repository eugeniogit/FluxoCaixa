using FluxoCaixa.Consolidado.Shared.Contracts.Messaging;
using Microsoft.Extensions.Options;

namespace FluxoCaixa.Consolidado.Shared.Infrastructure.Messaging;

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
		return _serviceProvider.GetRequiredService<LancamentosConsolidadosPublisher>();
    }

    public IMessageConsumer CreateConsumer()
    {
		return _serviceProvider.GetRequiredService<LancamentoConsumer>();
    }
}