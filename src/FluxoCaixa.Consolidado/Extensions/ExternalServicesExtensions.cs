using FluxoCaixa.Consolidado.Shared.Configurations;
using FluxoCaixa.Consolidado.Shared.Contracts.ExternalServices;
using FluxoCaixa.Consolidado.Shared.Infrastructure.ExternalServices;

namespace FluxoCaixa.Consolidado.Extensions;

public static class ExternalServicesExtensions
{
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LancamentoApiSettings>(
            configuration.GetSection("LancamentoApiSettings"));
        
        services.AddHttpClient<ILancamentoApiClient, LancamentoApiClient>();
        
        return services;
    }
}