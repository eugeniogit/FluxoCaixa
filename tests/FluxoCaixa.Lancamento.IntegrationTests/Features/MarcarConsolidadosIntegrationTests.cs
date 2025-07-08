using FluentAssertions;
using FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;
using FluxoCaixa.Lancamento.Shared.Domain.Entities;
using MongoDB.Driver;
using Xunit;

namespace FluxoCaixa.Lancamento.IntegrationTests.Features;

[Collection("LancamentoIntegrationTests")]
public class MarcarConsolidadosIntegrationTests : IClassFixture<LancamentoTestFactory>
{
    private readonly LancamentoTestFactory _factory;

    public MarcarConsolidadosIntegrationTests(LancamentoTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Handle_DeveMarcarComoConsolidado_QuandoLancamentoExiste()
    {
        // Arrange
        var lancamento = await TestHelpers.CreateLancamentoInDatabase(_factory);
        var command = TestHelpers.CreateMarcarConsolidadosCommand(lancamento.Id);
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Verify initial state
        lancamento.Consolidado.Should().BeFalse();

        // Act
        await mediator.Send(command);

        // Assert
        var lancamentoAtualizado = await dbContext.Lancamentos
            .Find(l => l.Id == lancamento.Id)
            .FirstOrDefaultAsync();

        lancamentoAtualizado.Should().NotBeNull();
        lancamentoAtualizado!.Consolidado.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveMarcarMultiplosLancamentos_QuandoTodosExistem()
    {
        // Arrange
        var lancamentos = await TestHelpers.CreateMultipleLancamentosInDatabase(_factory, 3);
        var command = TestHelpers.CreateMarcarConsolidadosCommand(lancamentos.Select(l => l.Id).ToArray());
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Verify initial state
        lancamentos.Should().AllSatisfy(l => l.Consolidado.Should().BeFalse());

        // Act
        await mediator.Send(command);

        // Assert
        var lancamentosAtualizados = await dbContext.Lancamentos
            .Find(l => lancamentos.Select(x => x.Id).Contains(l.Id))
            .ToListAsync();

        lancamentosAtualizados.Should().HaveCount(3);
        lancamentosAtualizados.Should().AllSatisfy(l => l.Consolidado.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_DeveMarcarApenasLancamentosExistentes_QuandoAlgunsNaoExistem()
    {
        // Arrange
        var lancamentoExistente = await TestHelpers.CreateLancamentoInDatabase(_factory);
        var idInexistente = "507f1f77bcf86cd799439011"; // ObjectId válido mas inexistente
        var command = TestHelpers.CreateMarcarConsolidadosCommand(lancamentoExistente.Id, idInexistente);
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Act
        await mediator.Send(command);

        // Assert
        var lancamentoAtualizado = await dbContext.Lancamentos
            .Find(l => l.Id == lancamentoExistente.Id)
            .FirstOrDefaultAsync();

        lancamentoAtualizado.Should().NotBeNull();
        lancamentoAtualizado!.Consolidado.Should().BeTrue();

        // Verify that non-existent ID doesn't cause issues
        var lancamentoInexistente = await dbContext.Lancamentos
            .Find(l => l.Id == idInexistente)
            .FirstOrDefaultAsync();

        lancamentoInexistente.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NaoDeveAlterarNada_QuandoListaVazia()
    {
        // Arrange
        var lancamento = await TestHelpers.CreateLancamentoInDatabase(_factory);
        var command = TestHelpers.CreateMarcarConsolidadosCommand();
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Act
        await mediator.Send(command);

        // Assert
        var lancamentoIналтеrado = await dbContext.Lancamentos
            .Find(l => l.Id == lancamento.Id)
            .FirstOrDefaultAsync();

        lancamentoIналтеrado.Should().NotBeNull();
        lancamentoIналтеrado!.Consolidado.Should().BeFalse(); // Should remain unchanged
    }

    [Fact]
    public async Task Handle_DeveMarcarApenasLancamentosNaoConsolidados_QuandoAlgunsJaConsolidados()
    {
        // Arrange
        var lancamentos = await TestHelpers.CreateMultipleLancamentosInDatabase(_factory, 3);
        var dbContext = _factory.GetDbContext();

        // Mark the first one as consolidated manually
        await dbContext.Lancamentos.UpdateOneAsync(
            l => l.Id == lancamentos[0].Id,
            Builders<Shared.Domain.Entities.Lancamento>.Update.Set(l => l.Consolidado, true));

        var command = TestHelpers.CreateMarcarConsolidadosCommand(lancamentos.Select(l => l.Id).ToArray());
        var mediator = _factory.GetMediator();

        // Act
        await mediator.Send(command);

        // Assert
        var lancamentosAtualizados = await dbContext.Lancamentos
            .Find(l => lancamentos.Select(x => x.Id).Contains(l.Id))
            .ToListAsync();

        lancamentosAtualizados.Should().HaveCount(3);
        lancamentosAtualizados.Should().AllSatisfy(l => l.Consolidado.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_DeveMarcarLancamentosDiferentesTipos_QuandoCredtsEDebits()
    {
        // Arrange
        var lancamentoCredito = await TestHelpers.CreateLancamentoInDatabase(
            _factory,
            tipo: TipoLancamento.Credito,
            valor: 100m);

        var lancamentoDebito = await TestHelpers.CreateLancamentoInDatabase(
            _factory,
            tipo: TipoLancamento.Debito,
            valor: 50m);

        var command = TestHelpers.CreateMarcarConsolidadosCommand(
            lancamentoCredito.Id,
            lancamentoDebito.Id);

        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Act
        await mediator.Send(command);

        // Assert
        var lancamentosAtualizados = await dbContext.Lancamentos
            .Find(l => l.Id == lancamentoCredito.Id || l.Id == lancamentoDebito.Id)
            .ToListAsync();

        lancamentosAtualizados.Should().HaveCount(2);
        lancamentosAtualizados.Should().AllSatisfy(l => l.Consolidado.Should().BeTrue());

        var creditoAtualizado = lancamentosAtualizados.First(l => l.Id == lancamentoCredito.Id);
        var debitoAtualizado = lancamentosAtualizados.First(l => l.Id == lancamentoDebito.Id);

        creditoAtualizado.Tipo.Should().Be(TipoLancamento.Credito);
        debitoAtualizado.Tipo.Should().Be(TipoLancamento.Debito);
    }

    [Fact]
    public async Task Handle_DeveMarcarLancamentosDiferentesComerciantes_QuandoMultiplosComerciantes()
    {
        // Arrange
        var lancamento1 = await TestHelpers.CreateLancamentoInDatabase(
            _factory,
            comerciante: "Comerciante A",
            valor: 100m);

        var lancamento2 = await TestHelpers.CreateLancamentoInDatabase(
            _factory,
            comerciante: "Comerciante B",
            valor: 200m);

        var command = TestHelpers.CreateMarcarConsolidadosCommand(
            lancamento1.Id,
            lancamento2.Id);

        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Act
        await mediator.Send(command);

        // Assert
        var lancamentosAtualizados = await dbContext.Lancamentos
            .Find(l => l.Id == lancamento1.Id || l.Id == lancamento2.Id)
            .ToListAsync();

        lancamentosAtualizados.Should().HaveCount(2);
        lancamentosAtualizados.Should().AllSatisfy(l => l.Consolidado.Should().BeTrue());

        var comercianteA = lancamentosAtualizados.First(l => l.Comerciante == "Comerciante A");
        var comercianteB = lancamentosAtualizados.First(l => l.Comerciante == "Comerciante B");

        comercianteA.Valor.Should().Be(100m);
        comercianteB.Valor.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_DeveProcessarGrandeQuantidadeLancamentos_QuandoMuitosIds()
    {
        // Arrange
        var lancamentos = await TestHelpers.CreateMultipleLancamentosInDatabase(_factory, 10);
        var command = TestHelpers.CreateMarcarConsolidadosCommand(lancamentos.Select(l => l.Id).ToArray());
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Act
        await mediator.Send(command);

        // Assert
        var lancamentosAtualizados = await dbContext.Lancamentos
            .Find(l => lancamentos.Select(x => x.Id).Contains(l.Id))
            .ToListAsync();

        lancamentosAtualizados.Should().HaveCount(10);
        lancamentosAtualizados.Should().AllSatisfy(l => l.Consolidado.Should().BeTrue());
    }
}