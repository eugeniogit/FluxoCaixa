using FluxoCaixa.Consolidado.Shared.Configurations;
using FluxoCaixa.Consolidado.Shared.Contracts.ExternalServices;
using FluxoCaixa.Consolidado.Shared.Domain.Events;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FluxoCaixa.Consolidado.Shared.Infrastructure.ExternalServices;

public class LancamentoApiClient : ILancamentoApiClient
{
    private readonly HttpClient _httpClient;
    private readonly LancamentoApiSettings _settings;
    private readonly ILogger<LancamentoApiClient> _logger;

    public LancamentoApiClient(
        HttpClient httpClient,
        IOptions<LancamentoApiSettings> options,
        ILogger<LancamentoApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
        }
    }

    public async Task<List<LancamentoEvent>> GetLancamentosByPeriodoAsync(
        DateTime dataInicio, 
        DateTime dataFim, 
        string? comerciante = null,
        bool? consolidado = null)
    {
        try
        {
            var queryString = $"dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}";
            
            if (!string.IsNullOrEmpty(comerciante))
            {
                queryString += $"&comerciante={Uri.EscapeDataString(comerciante)}";
            }

            if (consolidado.HasValue)
            {
                queryString += $"&consolidado={consolidado.Value.ToString().ToLower()}";
            }

            var response = await _httpClient.GetAsync($"/api/lancamentos?{queryString}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Erro de autenticação ao acessar API de Lançamentos. Verificar API Key.");
                throw new UnauthorizedAccessException("Falha na autenticação com a API de Lançamentos");
            }
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<ListarLancamentosResponse>(content, options);
            
            return result?.Lancamentos ?? new List<LancamentoEvent>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar lançamentos da API para período {DataInicio} até {DataFim} e comerciante {Comerciante}", 
                dataInicio, dataFim, comerciante);
            throw;
        }
    }

    public async Task MarcarLancamentosComoConsolidadosAsync(List<string> lancamentoIds)
    {
        try
        {
            var request = new MarcarConsolidadosRequest { LancamentoIds = lancamentoIds };
            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync("/api/lancamentos/marcar-consolidados", content);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Erro de autenticação ao marcar lançamentos como consolidados. Verificar API Key.");
                throw new UnauthorizedAccessException("Falha na autenticação com a API de Lançamentos");
            }
            
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Marcados {Count} lançamentos como consolidados", lancamentoIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar lançamentos como consolidados. IDs: {LancamentoIds}", 
                string.Join(", ", lancamentoIds));
            throw;
        }
    }
}

public class ListarLancamentosResponse
{
    public List<LancamentoEvent> Lancamentos { get; set; } = new();
}

public class MarcarConsolidadosRequest
{
    public List<string> LancamentoIds { get; set; } = new();
}