using FluxoCaixa.Consolidado.Infrastructure.Messaging;
using FluxoCaixa.Consolidado.Infrastructure.Messaging.Abstractions;

namespace FluxoCaixa.Consolidado.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MessageBrokerSettings>(
            configuration.GetSection("RabbitMqSettings"));
        
        services.AddSingleton<IMessageBrokerFactory, MessageBrokerFactory>();
        services.AddSingleton<LancamentosConsolidadosPublisher>();
        services.AddSingleton<LancamentoConsumer>();
        services.AddSingleton<IMessagePublisher>(provider => 
            provider.GetRequiredService<IMessageBrokerFactory>().CreatePublisher());
        services.AddSingleton<IMessageConsumer>(provider => 
            provider.GetRequiredService<IMessageBrokerFactory>().CreateConsumer());
        
        services.AddHostedService<LancamentoBackgroundService>();
        
        return services;
    }
}