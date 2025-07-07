using FluxoCaixa.Lancamento.Configuration;
using FluxoCaixa.Lancamento.Infrastructure.Messaging;

namespace FluxoCaixa.Lancamento.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(
            configuration.GetSection("RabbitMqSettings"));
        
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddSingleton<MarcarConsolidadosConsumer>();
        services.AddHostedService<MarcarConsolidadosBackgroundService>();
        
        return services;
    }
}