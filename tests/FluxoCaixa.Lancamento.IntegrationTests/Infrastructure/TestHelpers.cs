using FluxoCaixa.Lancamento.Features.CriarLancamento;
using FluxoCaixa.Lancamento.IntegrationTests.Extensions;
using FluentAssertions;
using System.Text.Json;
using System.Text;
using System.Net.Http.Json;

namespace FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;

public static class TestHelpers
{
    private static class TestConstants
    {
        public const int SmallTestDataCount = 2;
        public const int MediumTestDataCount = 5;
        public const int LargeTestDataCount = 10;
    }

    public static HttpClient CreateClientWithApiKey(this LancamentoTestFactory factory, string? apiKey = null)
    {
        var client = factory.CreateClient();
        client.AddApiKey(apiKey ?? factory.GetValidApiKey());
        return client;
    }

    public static async Task<List<CriarLancamentoResponse>> CreateTestLancamentos(
        this HttpClient client,
        IEnumerable<CriarLancamentoRequest> requests)
    {
        var responses = new List<CriarLancamentoResponse>();
        
        foreach (var request in requests)
        {
            var response = await client.PostAsJsonAsync("/api/lancamentos", request);
            response.EnsureSuccessStatusCode();
            
            var lancamento = await response.ReadAsJsonAsync<CriarLancamentoResponse>();
            lancamento.Should().NotBeNull();
            responses.Add(lancamento!);
        }
        
        return responses;
    }

    public static void AssertLancamentoResponse(
        CriarLancamentoResponse? response,
        CriarLancamentoRequest request)
    {
        response.Should().NotBeNull();
        response!.Id.Should().NotBeEmpty();
        response.Comerciante.Should().Be(request.Comerciante);
        response.Valor.Should().Be(request.Valor);
        response.Tipo.Should().Be(request.Tipo);
        response.Data.Should().Be(request.Data);
        response.Descricao.Should().Be(request.Descricao);
        response.DataLancamento.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    public static int GetSmallTestDataCount() => TestConstants.SmallTestDataCount;
    public static int GetMediumTestDataCount() => TestConstants.MediumTestDataCount;
    public static int GetLargeTestDataCount() => TestConstants.LargeTestDataCount;

    public static DateTime GetTestDate(int daysFromNow = 0)
    {
        return DateTime.Today.AddDays(daysFromNow);
    }

    public static DateTime GetSpecificTestDate()
    {
        return new DateTime(2024, 1, 15);
    }
}