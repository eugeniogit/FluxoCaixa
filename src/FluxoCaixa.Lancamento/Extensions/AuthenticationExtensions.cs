using FluxoCaixa.Lancamento.Shared.Configurations;
using FluxoCaixa.Lancamento.Shared.Infrastructure.Authentication;

namespace FluxoCaixa.Lancamento.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ApiKeySettings>(configuration.GetSection("ApiKeySettings"));

        services.AddAuthentication(ApiKeyAuthenticationSchemeOptions.DefaultScheme)
            .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationSchemeOptions.DefaultScheme, 
                null);

        services.AddAuthorization();

        return services;
    }
}