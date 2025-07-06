using FluentAssertions;
using FluxoCaixa.Lancamento.Domain;
using FluxoCaixa.Lancamento.Features.CriarLancamento;
using FluxoCaixa.Lancamento.Features.ListarLancamentos;
using FluxoCaixa.Lancamento.IntegrationTests.Extensions;
using FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;
using FluxoCaixa.Lancamento.IntegrationTests.TestData;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FluxoCaixa.Lancamento.IntegrationTests;

public class LancamentoIntegrationTests : IClassFixture<LancamentoTestFactory>
{
    private readonly LancamentoTestFactory _factory;
    private readonly HttpClient _client;

    public LancamentoIntegrationTests(LancamentoTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClientWithApiKey();
    }

    [Fact]
    public async Task CriarLancamento_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var request = LancamentoTestData.CreateValidLancamentoRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/lancamentos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var lancamento = await response.ReadAsJsonAsync<CriarLancamentoResponse>();
        lancamento.Should().NotBeNull();
        lancamento!.Id.Should().NotBeEmpty();
        lancamento.Comerciante.Should().Be(request.Comerciante);
        lancamento.Valor.Should().Be(request.Valor);
        lancamento.Tipo.Should().Be(request.Tipo);
        lancamento.Descricao.Should().Be(request.Descricao);
    }

    [Fact]
    public async Task CriarLancamento_WithoutApiKey_ShouldReturn401Unauthorized()
    {
        // Arrange
        var clientWithoutApiKey = _factory.CreateClient();
        var request = LancamentoTestData.CreateValidLancamentoRequest();

        // Act
        var response = await clientWithoutApiKey.PostAsJsonAsync("/api/lancamentos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CriarLancamento_WithInvalidApiKey_ShouldReturn401Unauthorized()
    {
        // Arrange
        var clientWithInvalidApiKey = _factory.CreateClient();
        clientWithInvalidApiKey.AddApiKey("invalid-api-key");
        var request = LancamentoTestData.CreateValidLancamentoRequest();

        // Act
        var response = await clientWithInvalidApiKey.PostAsJsonAsync("/api/lancamentos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", 100, TipoLancamento.Credito)] // Comerciante vazio
    [InlineData("Loja", 0, TipoLancamento.Credito)] // Valor zero
    [InlineData("Loja", -100, TipoLancamento.Credito)] // Valor negativo
    public async Task CriarLancamento_WithInvalidData_ShouldReturn400BadRequest(string comerciante, decimal valor, TipoLancamento tipo)
    {
        // Arrange
        var request = new CriarLancamentoRequest
        {
            Comerciante = comerciante,
            Valor = valor,
            Tipo = tipo,
            Data = DateTime.Now,
            Descricao = "Teste"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/lancamentos", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListarLancamentos_WithoutFilters_ShouldReturnLancamentos()
    {
        // Arrange
        var requests = LancamentoTestData.CreateMultipleLancamentoRequests(3);

        // Create some test data
        foreach (var request in requests)
        {
            await _client.PostAsJsonAsync("/api/lancamentos", request);
        }

        // Act
        var response = await _client.GetAsync("/api/lancamentos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<ListarLancamentosResponse>();
        result.Should().NotBeNull();
        result!.Lancamentos.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task ListarLancamentos_WithComercianteFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var comerciante = "Loja Teste Filtro";
        var requestsForComerciante = Enumerable.Range(0, 2)
            .Select(_ => LancamentoTestData.CreateLancamentoRequestForComerciante(comerciante))
            .ToList();

        var requestsForOtherComerciante = Enumerable.Range(0, 2)
            .Select(_ => LancamentoTestData.CreateLancamentoRequestForComerciante("Outra Loja"))
            .ToList();

        // Create test data
        foreach (var request in requestsForComerciante.Concat(requestsForOtherComerciante))
        {
            await _client.PostAsJsonAsync("/api/lancamentos", request);
        }

        // Act
        var response = await _client.GetAsync($"/api/lancamentos?comerciante={Uri.EscapeDataString(comerciante)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<ListarLancamentosResponse>();
        result.Should().NotBeNull();
        result!.Lancamentos.Should().OnlyContain(l => l.Comerciante == comerciante);
        result.Lancamentos.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListarLancamentos_WithDateFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var targetDate = new DateTime(2024, 1, 15);
        var dataInicio = targetDate.Date;
        var dataFim = targetDate.Date.AddDays(1).AddTicks(-1);

        var requestsForTargetDate = Enumerable.Range(0, 2)
            .Select(_ => LancamentoTestData.CreateLancamentoRequestForDate(targetDate))
            .ToList();

        var requestsForOtherDate = Enumerable.Range(0, 2)
            .Select(_ => LancamentoTestData.CreateLancamentoRequestForDate(targetDate.AddDays(5)))
            .ToList();

        // Create test data
        foreach (var request in requestsForTargetDate.Concat(requestsForOtherDate))
        {
            await _client.PostAsJsonAsync("/api/lancamentos", request);
        }

        // Act
        var response = await _client.GetAsync($"/api/lancamentos?dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<ListarLancamentosResponse>();
        result.Should().NotBeNull();
        result!.Lancamentos.Should().OnlyContain(l => l.Data.Date == targetDate.Date);
        result.Lancamentos.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListarLancamentos_WithTipoFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var targetTipo = TipoLancamento.Credito;

        var creditRequests = Enumerable.Range(0, 2)
            .Select(_ => LancamentoTestData.CreateLancamentoRequestWithSpecificData(
                "Loja Credito", 100, TipoLancamento.Credito, DateTime.Now))
            .ToList();

        var debitRequests = Enumerable.Range(0, 2)
            .Select(_ => LancamentoTestData.CreateLancamentoRequestWithSpecificData(
                "Loja Debito", 100, TipoLancamento.Debito, DateTime.Now))
            .ToList();

        // Create test data
        foreach (var request in creditRequests.Concat(debitRequests))
        {
            await _client.PostAsJsonAsync("/api/lancamentos", request);
        }

        // Act
        var response = await _client.GetAsync($"/api/lancamentos?tipo={targetTipo}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<ListarLancamentosResponse>();
        result.Should().NotBeNull();
        result!.Lancamentos.Should().OnlyContain(l => l.Tipo == targetTipo);
        result.Lancamentos.Where(l => l.Comerciante.Contains("Loja Credito")).Should().HaveCount(2);
    }

    [Fact]
    public async Task ListarLancamentos_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var requests = LancamentoTestData.CreateMultipleLancamentoRequests(10);

        // Create test data
        foreach (var request in requests)
        {
            await _client.PostAsJsonAsync("/api/lancamentos", request);
        }

        // Act - Get second page with 5 items per page
        var response = await _client.GetAsync("/api/lancamentos?pagina=2&tamanhoPagina=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.ReadAsJsonAsync<ListarLancamentosResponse>();
        result.Should().NotBeNull();
        result!.Lancamentos.Should().HaveCountLessOrEqualTo(5);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
        content.Should().Contain("FluxoCaixa.Lancamento");
    }

    [Fact]
    public async Task ReadinessCheck_ShouldReturnReady()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ready");
        content.Should().Contain("FluxoCaixa.Lancamento");
    }
}