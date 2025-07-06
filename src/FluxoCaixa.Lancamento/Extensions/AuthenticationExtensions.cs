using FluxoCaixa.Lancamento.Authentication;
using FluxoCaixa.Lancamento.Configuration;

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