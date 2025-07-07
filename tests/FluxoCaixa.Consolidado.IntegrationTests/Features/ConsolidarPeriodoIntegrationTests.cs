using FluentAssertions;
using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;
using FluxoCaixa.Consolidado.Infrastructure.ExternalServices;
using FluxoCaixa.Consolidado.IntegrationTests.Infrastructure;
using Moq;
using Xunit;

namespace FluxoCaixa.Consolidado.IntegrationTests.Features;

[Collection("ConsolidadoIntegrationTests")]
public class ConsolidarPeriodoIntegrationTests : IClassFixture<ConsolidadoTestFactory>
{
    private readonly ConsolidadoTestFactory _factory;

    public ConsolidarPeriodoIntegrationTests(ConsolidadoTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Handle_DeveConsolidarLancamentos_QuandoExistemLancamentosNaoConsolidados()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var dataInicio = DateTime.Today.AddDays(-2);
        var dataFim = DateTime.Today;
        var comerciante = "Comerciante Periodo";

        var lancamentos = new List<LancamentoEvent>
        {
            TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 100m,
                tipo: TipoLancamento.Credito,
                data: dataInicio),
            TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 50m,
                tipo: TipoLancamento.Debito,
                data: dataInicio),
            TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 75m,
                tipo: TipoLancamento.Credito,
                data: dataFim)
        };

        var mockApiClient = _factory.GetMockLancamentoApiClient();
        mockApiClient.Setup(x => x.GetLancamentosByPeriodoAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), false))
            .ReturnsAsync(lancamentos);

        var command = TestHelpers.CreateConsolidarPeriodoCommand(dataInicio, dataFim, comerciante);
        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidadoInicio = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, dataInicio);
        var consolidadoFim = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, dataFim);

        consolidadoInicio.Should().NotBeNull();
        consolidadoInicio!.TotalCreditos.Should().Be(100m);
        consolidadoInicio.TotalDebitos.Should().Be(50m);
        consolidadoInicio.SaldoLiquido.Should().Be(50m);

        consolidadoFim.Should().NotBeNull();
        consolidadoFim!.TotalCreditos.Should().Be(75m);
        consolidadoFim.TotalDebitos.Should().Be(0m);
        consolidadoFim.SaldoLiquido.Should().Be(75m);

        // Verify API was called correctly
        mockApiClient.Verify(x => x.GetLancamentosByPeriodoAsync(
            dataInicio, dataFim, comerciante, false), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveConsolidarMultiplosComerciantes_QuandoComercianteNaoEspecificado()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var dataInicio = DateTime.Today.AddDays(-1);
        var dataFim = DateTime.Today;

        var lancamentos = new List<LancamentoEvent>
        {
            TestHelpers.CreateLancamentoEvent(
                comerciante: "Comerciante A",
                valor: 200m,
                tipo: TipoLancamento.Credito,
                data: dataInicio),
            TestHelpers.CreateLancamentoEvent(
                comerciante: "Comerciante B",
                valor: 150m,
                tipo: TipoLancamento.Debito,
                data: dataInicio),
            TestHelpers.CreateLancamentoEvent(
                comerciante: "Comerciante A",
                valor: 100m,
                tipo: TipoLancamento.Debito,
                data: dataFim)
        };

        var mockApiClient = _factory.GetMockLancamentoApiClient();
        mockApiClient.Setup(x => x.GetLancamentosByPeriodoAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, false))
            .ReturnsAsync(lancamentos);

        var command = TestHelpers.CreateConsolidarPeriodoCommand(dataInicio, dataFim, null);
        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidadoA1 = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante A", dataInicio);
        var consolidadoA2 = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante A", dataFim);
        var consolidadoB = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante B", dataInicio);

        consolidadoA1.Should().NotBeNull();
        consolidadoA1!.TotalCreditos.Should().Be(200m);
        consolidadoA1.SaldoLiquido.Should().Be(200m);

        consolidadoA2.Should().NotBeNull();
        consolidadoA2!.TotalDebitos.Should().Be(100m);
        consolidadoA2.SaldoLiquido.Should().Be(-100m);

        consolidadoB.Should().NotBeNull();
        consolidadoB!.TotalDebitos.Should().Be(150m);
        consolidadoB.SaldoLiquido.Should().Be(-150m);
    }

    [Fact]
    public async Task Handle_DeveAtualizarConsolidadoExistente_QuandoJaExisteConsolidado()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var data = DateTime.Today;
        var comerciante = "Comerciante Existente";

        // Create existing consolidado
        await TestHelpers.CreateConsolidadoInDatabase(
            _factory, 
            comerciante, 
            data, 
            totalCreditos: 50m, 
            quantidadeCreditos: 1);

        var lancamentos = new List<LancamentoEvent>
        {
            TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 100m,
                tipo: TipoLancamento.Credito,
                data: data),
            TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 25m,
                tipo: TipoLancamento.Debito,
                data: data)
        };

        var mockApiClient = _factory.GetMockLancamentoApiClient();
        mockApiClient.Setup(x => x.GetLancamentosByPeriodoAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), comerciante, false))
            .ReturnsAsync(lancamentos);

        var command = TestHelpers.CreateConsolidarPeriodoCommand(data, data, comerciante);
        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidadoAtualizado = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, data);

        consolidadoAtualizado.Should().NotBeNull();
        consolidadoAtualizado!.TotalCreditos.Should().Be(150m); // 50 + 100
        consolidadoAtualizado.TotalDebitos.Should().Be(25m);
        consolidadoAtualizado.SaldoLiquido.Should().Be(125m); // 150 - 25
        consolidadoAtualizado.QuantidadeCreditos.Should().Be(2); // 1 + 1
        consolidadoAtualizado.QuantidadeDebitos.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NaoDeveProcessarNada_QuandoNaoExistemLancamentos()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var dataInicio = DateTime.Today.AddDays(-1);
        var dataFim = DateTime.Today;

        var mockApiClient = _factory.GetMockLancamentoApiClient();
        mockApiClient.Setup(x => x.GetLancamentosByPeriodoAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), false))
            .ReturnsAsync(new List<LancamentoEvent>());

        var command = TestHelpers.CreateConsolidarPeriodoCommand(dataInicio, dataFim);
        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidados = await TestHelpers.GetAllConsolidados(_factory);
        consolidados.Should().BeEmpty();

        // Verify API was called
        mockApiClient.Verify(x => x.GetLancamentosByPeriodoAsync(
            dataInicio, dataFim, null, false), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveAgruparLancamentosPorComercianteEData_QuandoMultiplosLancamentos()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var data1 = DateTime.Today.AddDays(-1);
        var data2 = DateTime.Today;
        var comerciante = "Comerciante Agrupado";

        var lancamentos = new List<LancamentoEvent>
        {
            // Mesmo comerciante, mesma data - devem ser agrupados
            TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 100m,
                tipo: TipoLancamento.Credito,
                data: data1),
            TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 50m,
                tipo: TipoLancamento.Debito,
                data: data1),
            
            // Mesmo comerciante, data diferente - consolidado separado
            TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 75m,
                tipo: TipoLancamento.Credito,
                data: data2)
        };

        var mockApiClient = _factory.GetMockLancamentoApiClient();
        mockApiClient.Setup(x => x.GetLancamentosByPeriodoAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), false))
            .ReturnsAsync(lancamentos);

        var command = TestHelpers.CreateConsolidarPeriodoCommand(data1, data2);
        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidado1 = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, data1);
        var consolidado2 = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, data2);

        consolidado1.Should().NotBeNull();
        consolidado1!.TotalCreditos.Should().Be(100m);
        consolidado1.TotalDebitos.Should().Be(50m);
        consolidado1.SaldoLiquido.Should().Be(50m);
        consolidado1.QuantidadeCreditos.Should().Be(1);
        consolidado1.QuantidadeDebitos.Should().Be(1);

        consolidado2.Should().NotBeNull();
        consolidado2!.TotalCreditos.Should().Be(75m);
        consolidado2.TotalDebitos.Should().Be(0m);
        consolidado2.SaldoLiquido.Should().Be(75m);
        consolidado2.QuantidadeCreditos.Should().Be(1);
        consolidado2.QuantidadeDebitos.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DeveProcessarGrandeQuantidadeLancamentos_QuandoMuitosLancamentos()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var data = DateTime.Today;
        var comerciante = "Comerciante Grande Volume";

        // Create 100 lancamentos
        var lancamentos = await TestHelpers.CreateMultipleLancamentoEvents(
            100, 
            comerciante, 
            data);

        var mockApiClient = _factory.GetMockLancamentoApiClient();
        mockApiClient.Setup(x => x.GetLancamentosByPeriodoAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), false))
            .ReturnsAsync(lancamentos);

        var command = TestHelpers.CreateConsolidarPeriodoCommand(data, data);
        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidados = await TestHelpers.GetAllConsolidados(_factory);
        consolidados.Should().HaveCount(100); // One per day per comerciante

        // Verify totals for the specific comerciante on specific date
        var consolidadoEspecifico = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, data);
        consolidadoEspecifico.Should().NotBeNull();
        consolidadoEspecifico!.QuantidadeCreditos.Should().Be(1);
        consolidadoEspecifico.QuantidadeDebitos.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeveProcessarPeriodoCompleto_QuandoDataInicioEDataFimDiferentes()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var dataInicio = DateTime.Today.AddDays(-5);
        var dataFim = DateTime.Today;
        var comerciante = "Comerciante Periodo Completo";

        var lancamentos = new List<LancamentoEvent>();
        
        // Create lancamentos for each day in the period
        for (var date = dataInicio; date <= dataFim; date = date.AddDays(1))
        {
            lancamentos.Add(TestHelpers.CreateLancamentoEvent(
                comerciante: comerciante,
                valor: 100m,
                tipo: TipoLancamento.Credito,
                data: date));
        }

        var mockApiClient = _factory.GetMockLancamentoApiClient();
        mockApiClient.Setup(x => x.GetLancamentosByPeriodoAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), comerciante, false))
            .ReturnsAsync(lancamentos);

        var command = TestHelpers.CreateConsolidarPeriodoCommand(dataInicio, dataFim, comerciante);
        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidados = await TestHelpers.GetAllConsolidados(_factory);
        consolidados.Should().HaveCount(6); // 6 days

        foreach (var consolidado in consolidados)
        {
            consolidado.Comerciante.Should().Be(comerciante);
            consolidado.TotalCreditos.Should().Be(100m);
            consolidado.SaldoLiquido.Should().Be(100m);
            consolidado.QuantidadeCreditos.Should().Be(1);
        }
    }

    [Fact]
    public async Task Handle_DeveFiltrarPorComercianteEspecifico_QuandoComercianteInformado()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var data = DateTime.Today;
        var comercianteEspecifico = "Comerciante Especifico";

        var command = TestHelpers.CreateConsolidarPeriodoCommand(data, data, comercianteEspecifico);
        var mediator = _factory.GetMediator();

        var mockApiClient = _factory.GetMockLancamentoApiClient();
        mockApiClient.Setup(x => x.GetLancamentosByPeriodoAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), comercianteEspecifico, false))
            .ReturnsAsync(new List<LancamentoEvent>());

        // Act
        await mediator.Send(command);

        // Assert
        mockApiClient.Verify(x => x.GetLancamentosByPeriodoAsync(
            data, data, comercianteEspecifico, false), Times.Once);
    }
}