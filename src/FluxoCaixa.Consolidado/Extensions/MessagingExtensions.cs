using FluxoCaixa.Consolidado.Configuration;
using FluxoCaixa.Consolidado.Infrastructure.Messaging;

namespace FluxoCaixa.Consolidado.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(
            configuration.GetSection("RabbitMqSettings"));
        
        services.AddSingleton<IRabbitMqConsumer, RabbitMqConsumer>();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddHostedService<RabbitMqBackgroundService>();
        
        return services;
    }
}