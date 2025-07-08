using FluentAssertions;
using FluxoCaixa.Consolidado.Features.ConsolidarLancamento;
using FluxoCaixa.Consolidado.IntegrationTests.Infrastructure;
using FluxoCaixa.Consolidado.Shared.Domain.Events;
using Xunit;

namespace FluxoCaixa.Consolidado.IntegrationTests.Features;

[Collection("ConsolidadoIntegrationTests")]
public class ConsolidarLancamentoIntegrationTests : IClassFixture<ConsolidadoTestFactory>, IAsyncLifetime
{
    private readonly ConsolidadoTestFactory _factory;

    public ConsolidarLancamentoIntegrationTests(ConsolidadoTestFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await TestHelpers.ClearDatabase(_factory);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Handle_DeveCriarNovoConsolidado_QuandoNaoExiste()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var command = TestHelpers.CreateConsolidarLancamentoCommand(
            comerciante: "Comerciante Novo",
            valor: 150m,
            tipo: TipoLancamento.Credito,
            data: DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));

        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidado = await TestHelpers.GetConsolidadoFromDatabase(
            _factory, 
            "Comerciante Novo", 
            DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));

        consolidado.Should().NotBeNull();
        consolidado!.Comerciante.Should().Be("Comerciante Novo");
        consolidado.Data.Should().Be(DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));
        consolidado.TotalCreditos.Should().Be(150m);
        consolidado.TotalDebitos.Should().Be(0m);
        consolidado.SaldoLiquido.Should().Be(150m);
        consolidado.QuantidadeCreditos.Should().Be(1);
        consolidado.QuantidadeDebitos.Should().Be(0);

        // Verify idempotency - lancamento marked as consolidated
        var isConsolidado = await TestHelpers.IsLancamentoConsolidado(_factory, command.LancamentoEvent.Id);
        isConsolidado.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveAtualizarConsolidadoExistente_QuandoJaExiste()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var comerciante = "Comerciante Existente";
        var data = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        // Create existing consolidado
        var consolidadoExistente = await TestHelpers.CreateConsolidadoInDatabase(
            _factory, 
            comerciante, 
            data, 
            totalCreditos: 100m, 
            quantidadeCreditos: 1);

        var command = TestHelpers.CreateConsolidarLancamentoCommand(
            comerciante: comerciante,
            valor: 75m,
            tipo: TipoLancamento.Debito,
            data: data);

        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidadoAtualizado = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, data);

        consolidadoAtualizado.Should().NotBeNull();
        consolidadoAtualizado!.TotalCreditos.Should().Be(100m);
        consolidadoAtualizado.TotalDebitos.Should().Be(75m);
        consolidadoAtualizado.SaldoLiquido.Should().Be(25m); // 100 - 75
        consolidadoAtualizado.QuantidadeCreditos.Should().Be(1);
        consolidadoAtualizado.QuantidadeDebitos.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeveProcessarLancamentoCredito_QuandoTipoCredito()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var command = TestHelpers.CreateConsolidarLancamentoCommand(
            comerciante: "Comerciante Credito",
            valor: 200m,
            tipo: TipoLancamento.Credito,
            data: DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));

        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidado = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante Credito", DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));

        consolidado.Should().NotBeNull();
        consolidado!.TotalCreditos.Should().Be(200m);
        consolidado.TotalDebitos.Should().Be(0m);
        consolidado.SaldoLiquido.Should().Be(200m);
        consolidado.QuantidadeCreditos.Should().Be(1);
        consolidado.QuantidadeDebitos.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DeveProcessarLancamentoDebito_QuandoTipoDebito()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var command = TestHelpers.CreateConsolidarLancamentoCommand(
            comerciante: "Comerciante Debito",
            valor: 80m,
            tipo: TipoLancamento.Debito,
            data: DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));

        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidado = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante Debito", DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));

        consolidado.Should().NotBeNull();
        consolidado!.TotalCreditos.Should().Be(0m);
        consolidado!.TotalDebitos.Should().Be(80m);
        consolidado.SaldoLiquido.Should().Be(-80m);
        consolidado.QuantidadeCreditos.Should().Be(0);
        consolidado.QuantidadeDebitos.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeveIgnorarLancamentoDuplicado_QuandoJaProcessado()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var lancamentoId = Guid.NewGuid().ToString();
        var command = TestHelpers.CreateConsolidarLancamentoCommand(
            id: lancamentoId,
            comerciante: "Comerciante Duplicado",
            valor: 50m,
            tipo: TipoLancamento.Credito,
            data: DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));

        var mediator = _factory.GetMediator();

        // Process first time
        await mediator.Send(command);

        // Act - Process same lancamento again
        await mediator.Send(command);

        // Assert
        var consolidado = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante Duplicado", DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc));

        consolidado.Should().NotBeNull();
        consolidado!.TotalCreditos.Should().Be(50m); // Should not be doubled
        consolidado.QuantidadeCreditos.Should().Be(1); // Should not be doubled
        consolidado.SaldoLiquido.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_DeveDefinirDataConsolidacaoSemHorario_QuandoProcessado()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var dataComHorario = new DateTime(2023, 12, 25, 14, 30, 45);
        var command = TestHelpers.CreateConsolidarLancamentoCommand(
            comerciante: "Comerciante Data",
            data: dataComHorario);

        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var consolidado = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante Data", DateTime.SpecifyKind(dataComHorario.Date, DateTimeKind.Utc));

        consolidado.Should().NotBeNull();
        consolidado!.Data.Should().Be(DateTime.SpecifyKind(dataComHorario.Date, DateTimeKind.Utc));
        consolidado.Data.Hour.Should().Be(0);
        consolidado.Data.Minute.Should().Be(0);
        consolidado.Data.Second.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DeveProcessarMultiplosLancamentosParaMesmoComercianteEData()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var comerciante = "Comerciante Multiplos";
        var data = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var commands = new[]
        {
            TestHelpers.CreateConsolidarLancamentoCommand(
                comerciante: comerciante,
                valor: 100m,
                tipo: TipoLancamento.Credito,
                data: data),
            TestHelpers.CreateConsolidarLancamentoCommand(
                comerciante: comerciante,
                valor: 50m,
                tipo: TipoLancamento.Debito,
                data: data),
            TestHelpers.CreateConsolidarLancamentoCommand(
                comerciante: comerciante,
                valor: 25m,
                tipo: TipoLancamento.Credito,
                data: data)
        };

        var mediator = _factory.GetMediator();

        // Act
        foreach (var command in commands)
        {
            await mediator.Send(command);
        }

        // Assert
        var consolidado = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, data);

        consolidado.Should().NotBeNull();
        consolidado!.TotalCreditos.Should().Be(125m); // 100 + 25
        consolidado.TotalDebitos.Should().Be(50m);
        consolidado.SaldoLiquido.Should().Be(75m); // 125 - 50
        consolidado.QuantidadeCreditos.Should().Be(2);
        consolidado.QuantidadeDebitos.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeveCriarConsolidadosSeparados_QuandoDiferentesComerciantes()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var data = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var commands = new[]
        {
            TestHelpers.CreateConsolidarLancamentoCommand(
                comerciante: "Comerciante A",
                valor: 100m,
                tipo: TipoLancamento.Credito,
                data: data),
            TestHelpers.CreateConsolidarLancamentoCommand(
                comerciante: "Comerciante B",
                valor: 200m,
                tipo: TipoLancamento.Debito,
                data: data)
        };

        var mediator = _factory.GetMediator();

        // Act
        foreach (var command in commands)
        {
            await mediator.Send(command);
        }

        // Assert
        var consolidadoA = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante A", data);
        var consolidadoB = await TestHelpers.GetConsolidadoFromDatabase(_factory, "Comerciante B", data);

        consolidadoA.Should().NotBeNull();
        consolidadoA!.TotalCreditos.Should().Be(100m);
        consolidadoA.SaldoLiquido.Should().Be(100m);

        consolidadoB.Should().NotBeNull();
        consolidadoB!.TotalDebitos.Should().Be(200m);
        consolidadoB.SaldoLiquido.Should().Be(-200m);
    }

    [Fact]
    public async Task Handle_DeveCriarConsolidadosSeparados_QuandoDiferentesDatas()
    {
        // Arrange
        await TestHelpers.ClearDatabase(_factory);
        var comerciante = "Comerciante Datas";
        var data1 = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var data2 = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc).AddDays(1);

        var commands = new[]
        {
            TestHelpers.CreateConsolidarLancamentoCommand(
                comerciante: comerciante,
                valor: 100m,
                tipo: TipoLancamento.Credito,
                data: data1),
            TestHelpers.CreateConsolidarLancamentoCommand(
                comerciante: comerciante,
                valor: 150m,
                tipo: TipoLancamento.Credito,
                data: data2)
        };

        var mediator = _factory.GetMediator();

        // Act
        foreach (var command in commands)
        {
            await mediator.Send(command);
        }

        // Assert
        var consolidado1 = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, data1);
        var consolidado2 = await TestHelpers.GetConsolidadoFromDatabase(_factory, comerciante, data2);

        consolidado1.Should().NotBeNull();
        consolidado1!.TotalCreditos.Should().Be(100m);
        consolidado1.Data.Should().Be(data1);

        consolidado2.Should().NotBeNull();
        consolidado2!.TotalCreditos.Should().Be(150m);
        consolidado2.Data.Should().Be(data2);
    }
}