using FluentAssertions;
using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;
using FluxoCaixa.Consolidado.Infrastructure.Database;
using FluxoCaixa.Consolidado.IntegrationTests.Extensions;
using FluxoCaixa.Consolidado.IntegrationTests.Infrastructure;
using FluxoCaixa.Consolidado.IntegrationTests.TestData;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.IntegrationTests;

public class ConsolidadoIntegrationTests : IClassFixture<ConsolidadoTestFactory>
{
    private readonly ConsolidadoTestFactory _factory;
    private readonly HttpClient _client;

    public ConsolidadoIntegrationTests(ConsolidadoTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ConsolidarPeriodo_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var testDate = TestHelpers.GetTestDate();
        var request = ConsolidadoTestData.CreateConsolidarPeriodoRequest(testDate);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consolidado/consolidar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Consolidação executada com sucesso");
    }

    [Fact]
    public async Task ConsolidarPeriodo_WithSpecificComerciante_ShouldReturnSuccess()
    {
        // Arrange
        var comerciante = "Loja Teste Específica";
        var testDate = TestHelpers.GetTestDate();
        var request = ConsolidadoTestData.CreateConsolidarPeriodoRequest(testDate, testDate, comerciante);

        // Act
        var response = await _client.PostAsJsonAsync("/api/consolidado/consolidar", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConsolidarPeriodo_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidRequest = new
        {
            dataInicio = "",  // Invalid date
            dataFim = "",     // Invalid date
            comerciante = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/consolidado/consolidar", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StatusConsolidacao_WithValidDate_ShouldReturnStatus()
    {
        // Arrange
        var testDate = TestHelpers.GetTestDate();

        // Act
        var response = await _client.GetAsync($"/api/consolidado/status/{testDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status da consolidação");
    }

    [Fact]
    public async Task ProcessarLancamento_ShouldCreateOrUpdateConsolidado()
    {
        // Arrange
        var (scope, context, mediator, repository) = await TestHelpers.CreateTestScope(_factory);
        using (scope)
        {
            var comerciante = "Loja Teste Processamento";
            var testDate = TestHelpers.GetTestDate();
            var lancamentoEvent = ConsolidadoTestData.CreateLancamentoEvent(comerciante, 100, TipoLancamento.Credito, testDate);

            var command = new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand
            {
                LancamentoEvent = lancamentoEvent
            };

            // Clean up any existing data
            await TestHelpers.CleanupDatabase(context);

            // Act
            await mediator.Send(command);

            // Assert
            var consolidado = context.ConsolidadosDiarios
                .FirstOrDefault(c => c.Comerciante == comerciante && c.Data == testDate.Date);

            TestHelpers.AssertConsolidadoValues(consolidado, 100, 0, 1, 0);
        }
    }

    [Fact]
    public async Task ProcessarMultiplosLancamentos_ShouldAggregateCorrectly()
    {
        // Arrange
        var (scope, context, mediator, repository) = await TestHelpers.CreateTestScope(_factory);
        using (scope)
        {
            var comerciante = "Loja Teste Múltiplos";
            var testDate = TestHelpers.GetTestDate();

            var creditoEvent1 = ConsolidadoTestData.CreateLancamentoEvent(comerciante, 100, TipoLancamento.Credito, testDate);
            var creditoEvent2 = ConsolidadoTestData.CreateLancamentoEvent(comerciante, 200, TipoLancamento.Credito, testDate);
            var debitoEvent = ConsolidadoTestData.CreateLancamentoEvent(comerciante, 50, TipoLancamento.Debito, testDate);

            // Clean up any existing data
            await TestHelpers.CleanupDatabase(context);

            // Act
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = creditoEvent1 });
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = creditoEvent2 });
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = debitoEvent });

            // Assert
            var consolidado = context.ConsolidadosDiarios
                .FirstOrDefault(c => c.Comerciante == comerciante && c.Data == testDate.Date);

            TestHelpers.AssertConsolidadoValues(consolidado, 300, 50, 2, 1); // 300 créditos, 50 débitos, 2 créditos count, 1 débito count
        }
    }

    [Fact]
    public async Task ProcessarLancamentos_DiferentesComerciantes_ShouldCreateSeparateConsolidados()
    {
        // Arrange
        var (scope, context, mediator, repository) = await TestHelpers.CreateTestScope(_factory);
        using (scope)
        {
            var comerciante1 = "Loja A";
            var comerciante2 = "Loja B";
            var testDate = TestHelpers.GetTestDate();

            var event1 = ConsolidadoTestData.CreateLancamentoEvent(comerciante1, 100, TipoLancamento.Credito, testDate);
            var event2 = ConsolidadoTestData.CreateLancamentoEvent(comerciante2, 200, TipoLancamento.Credito, testDate);

            // Clean up any existing data
            await TestHelpers.CleanupDatabase(context);

            // Act
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = event1 });
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = event2 });

            // Assert
            var consolidados = context.ConsolidadosDiarios
                .Where(c => c.Data == testDate.Date && (c.Comerciante == comerciante1 || c.Comerciante == comerciante2))
                .ToList();

            consolidados.Should().HaveCount(2);
            
            var consolidado1 = consolidados.First(c => c.Comerciante == comerciante1);
            var consolidado2 = consolidados.First(c => c.Comerciante == comerciante2);

            TestHelpers.AssertConsolidadoValues(consolidado1, 100, 0, 1, 0);
            TestHelpers.AssertConsolidadoValues(consolidado2, 200, 0, 1, 0);
        }
    }

    [Fact]
    public async Task TestApiAuthentication_ShouldReturnResult()
    {
        // Act
        var response = await _client.GetAsync("/api/test/auth");

        // Assert
        // This might fail if the Lancamento API is not running, but should not throw an exception
        response.Should().NotBeNull();
        
        // If the API is not available, we expect either Unauthorized or Problem response
        var validStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError };
        validStatusCodes.Should().Contain(response.StatusCode);
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
        content.Should().Contain("FluxoCaixa.Consolidado");
    }

    [Fact]
    public async Task Database_ShouldBeAccessible()
    {
        // Arrange
        var (scope, context, _, _) = await TestHelpers.CreateTestScope(_factory);
        using (scope)
        {
            // Act & Assert
            var canConnect = await context.Database.CanConnectAsync();
            canConnect.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ConsolidadoDiario_CRUD_Operations_ShouldWork()
    {
        // Arrange
        var (scope, context, _, _) = await TestHelpers.CreateTestScope(_factory);
        using (scope)
        {
            var testDate = TestHelpers.GetTestDate();
            var comerciante = "Loja CRUD Test";
            
            // Clean up any existing data
            await TestHelpers.CleanupDatabase(context);
            
            // Create consolidado using the new domain constructor
            var consolidado = new ConsolidadoDiario(comerciante, testDate);

            // Act - Create
            context.ConsolidadosDiarios.Add(consolidado);
            await context.SaveChangesAsync();

            // Assert - Read
            var savedConsolidado = await context.ConsolidadosDiarios
                .FirstOrDefaultAsync(c => c.Comerciante == comerciante && c.Data == testDate);

            savedConsolidado.Should().NotBeNull();
            savedConsolidado!.TotalCreditos.Should().Be(0);
            savedConsolidado.TotalDebitos.Should().Be(0);
            savedConsolidado.SaldoLiquido.Should().Be(0);
            savedConsolidado.QuantidadeCreditos.Should().Be(0);
            savedConsolidado.QuantidadeDebitos.Should().Be(0);

            // Act - Update using domain methods
            savedConsolidado.AdicionarCredito(100m);
            savedConsolidado.AdicionarDebito(50m);
            await context.SaveChangesAsync();

            // Assert - Updated
            var updatedConsolidado = await context.ConsolidadosDiarios
                .FirstOrDefaultAsync(c => c.Id == savedConsolidado.Id);

            updatedConsolidado.Should().NotBeNull();
            updatedConsolidado!.TotalCreditos.Should().Be(100m);
            updatedConsolidado.TotalDebitos.Should().Be(50m);
            updatedConsolidado.SaldoLiquido.Should().Be(50m); // Calculated property
            updatedConsolidado.QuantidadeCreditos.Should().Be(1);
            updatedConsolidado.QuantidadeDebitos.Should().Be(1);

            // Act - More updates
            updatedConsolidado.AdicionarCredito(200m);
            await context.SaveChangesAsync();

            // Assert - Additional updates
            var reloadedConsolidado = await context.ConsolidadosDiarios
                .FirstOrDefaultAsync(c => c.Id == savedConsolidado.Id);

            reloadedConsolidado!.TotalCreditos.Should().Be(300m); // 100 + 200
            reloadedConsolidado.SaldoLiquido.Should().Be(250m); // 300 - 50
            reloadedConsolidado.QuantidadeCreditos.Should().Be(2);

            // Act - Delete
            context.ConsolidadosDiarios.Remove(reloadedConsolidado);
            await context.SaveChangesAsync();

            // Assert - Deleted
            var deletedConsolidado = await context.ConsolidadosDiarios
                .FirstOrDefaultAsync(c => c.Id == savedConsolidado.Id);

            deletedConsolidado.Should().BeNull();
        }
    }

    [Fact]
    public async Task ProcessarLancamento_DuplicateMessage_ShouldBeIdempotent()
    {
        // Arrange
        var (scope, context, mediator, repository) = await TestHelpers.CreateTestScope(_factory);
        using (scope)
        {
            var comerciante = "Loja Teste Idempotência";
            var testDate = TestHelpers.GetTestDate();
            var lancamentoEvent = ConsolidadoTestData.CreateLancamentoEvent(comerciante, 100, TipoLancamento.Credito, testDate);

            var command = new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand
            {
                LancamentoEvent = lancamentoEvent
            };

            // Clean up any existing data
            await TestHelpers.CleanupDatabase(context);

            // Act - Process same message twice
            await mediator.Send(command);
            await mediator.Send(command); // Second time should be ignored

            // Assert - Should have processed only once
            var consolidado = context.ConsolidadosDiarios
                .FirstOrDefault(c => c.Comerciante == comerciante && c.Data == testDate.Date);

            TestHelpers.AssertConsolidadoValues(consolidado, 100, 0, 1, 0); // Should still have only one credit

            // Assert - Verify launch was marked as processed
            var lancamentoProcessado = context.LancamentosProcessados
                .FirstOrDefault(lp => lp.LancamentoId == lancamentoEvent.Id);

            lancamentoProcessado.Should().NotBeNull();
            lancamentoProcessado!.LancamentoId.Should().Be(lancamentoEvent.Id);
            lancamentoProcessado.DataProcessamento.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }
    }

    [Fact]
    public async Task ProcessarLancamento_MultipleDifferentMessages_ShouldProcessAll()
    {
        // Arrange
        var (scope, context, mediator, repository) = await TestHelpers.CreateTestScope(_factory);
        using (scope)
        {
            var comerciante = "Loja Teste Múltiplas Mensagens";
            var testDate = TestHelpers.GetTestDate();
            
            var lancamento1 = ConsolidadoTestData.CreateLancamentoEvent(comerciante, 100, TipoLancamento.Credito, testDate);
            var lancamento2 = ConsolidadoTestData.CreateLancamentoEvent(comerciante, 200, TipoLancamento.Credito, testDate);
            var lancamento3 = ConsolidadoTestData.CreateLancamentoEvent(comerciante, 50, TipoLancamento.Debito, testDate);

            // Clean up any existing data
            await TestHelpers.CleanupDatabase(context);

            // Act - Process different messages
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = lancamento1 });
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = lancamento2 });
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = lancamento3 });

            // Act - Try to process duplicates (should be ignored)
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = lancamento1 });
            await mediator.Send(new FluxoCaixa.Consolidado.Features.ProcessarLancamento.ProcessarLancamentoCommand { LancamentoEvent = lancamento2 });

            // Assert - Should have correct consolidated values
            var consolidado = context.ConsolidadosDiarios
                .FirstOrDefault(c => c.Comerciante == comerciante && c.Data == testDate.Date);

            TestHelpers.AssertConsolidadoValues(consolidado, 300, 50, 2, 1); // 300 credits, 50 debits

            // Assert - All launches should be marked as processed
            var processedCount = context.LancamentosProcessados
                .Where(lp => lp.LancamentoId == lancamento1.Id || 
                            lp.LancamentoId == lancamento2.Id || 
                            lp.LancamentoId == lancamento3.Id)
                .Count();

            processedCount.Should().Be(3); // All three different launches should be tracked
        }
    }
}