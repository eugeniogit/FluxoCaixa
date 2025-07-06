using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FluxoCaixa.Lancamento.IntegrationTests.Extensions;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void AddApiKey(this HttpClient client, string apiKey)
    {
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string uri, T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(uri, content);
    }

    public static async Task<T?> ReadAsJsonAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }
}