using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluxoCaixa.Lancamento.Shared.Configurations;

namespace FluxoCaixa.Lancamento.Shared.Infrastructure.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly ApiKeySettings _apiKeySettings;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IOptions<ApiKeySettings> apiKeySettings)
        : base(options, logger, encoder, clock)
    {
        _apiKeySettings = apiKeySettings.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key não fornecida"));
        }

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

        if (string.IsNullOrEmpty(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key vazia"));
        }

        var validApiKey = _apiKeySettings.ValidApiKeys.FirstOrDefault(k => k.Key == providedApiKey);

        if (validApiKey == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key inválida"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, validApiKey.Name),
            new Claim("ApiKeyId", validApiKey.Id),
            new Claim("ApiKey", providedApiKey)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation("Autenticação bem-sucedida para API Key: {ApiKeyName}", validApiKey.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}