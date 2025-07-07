using FluxoCaixa.Lancamento.Infrastructure.Messaging;
using FluxoCaixa.Lancamento.Infrastructure.Messaging.Abstractions;

namespace FluxoCaixa.Lancamento.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MessageBrokerSettings>(
            configuration.GetSection("RabbitMqSettings"));
        
        services.AddSingleton<IMessageBrokerFactory, MessageBrokerFactory>();
        services.AddSingleton<LancamentoPublisher>();
        services.AddSingleton<LancamentosConsolidadosConsumer>();
        services.AddSingleton<IMessagePublisher>(provider => 
            provider.GetRequiredService<IMessageBrokerFactory>().CreatePublisher());
        services.AddSingleton<IMessageConsumer>(provider => 
            provider.GetRequiredService<IMessageBrokerFactory>().CreateConsumer());
        
        services.AddHostedService<LancamentosConsolidadosBackgroundService>();
        
        return services;
    }
}