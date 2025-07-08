using FluentAssertions;
using FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;
using FluxoCaixa.Lancamento.Shared.Domain.Entities;
using MongoDB.Driver;
using Xunit;

namespace FluxoCaixa.Lancamento.IntegrationTests.Features;

[Collection("LancamentoIntegrationTests")]
public class CriarLancamentoIntegrationTests : IClassFixture<LancamentoTestFactory>, IAsyncLifetime
{
    private readonly LancamentoTestFactory _factory;

    public CriarLancamentoIntegrationTests(LancamentoTestFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ClearDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Handle_DeveSerCriadoComSucesso_QuandoComandoValido()
    {
        // Arrange
        var command = TestHelpers.CreateValidCriarLancamentoCommand();
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Act
        var response = await mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeNullOrEmpty();
        response.Comerciante.Should().Be(command.Comerciante);
        response.Valor.Should().Be(command.Valor);
        response.Tipo.Should().Be(command.Tipo);
        response.Data.Should().Be(command.Data);
        response.Descricao.Should().Be(command.Descricao);
        response.DataLancamento.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify database
        var lancamentoInDb = await dbContext.Lancamentos
            .Find(l => l.Id == response.Id)
            .FirstOrDefaultAsync();

        lancamentoInDb.Should().NotBeNull();
        lancamentoInDb!.Comerciante.Should().Be(command.Comerciante);
        lancamentoInDb.Valor.Should().Be(command.Valor);
        lancamentoInDb.Tipo.Should().Be(command.Tipo);
        lancamentoInDb.Data.Should().BeCloseTo(command.Data, TimeSpan.FromSeconds(1));
        lancamentoInDb.Descricao.Should().Be(command.Descricao);
        lancamentoInDb.Consolidado.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeveSerCriadoComSucesso_QuandoTipoCredito()
    {
        // Arrange
        var command = TestHelpers.CreateValidCriarLancamentoCommand(
            tipo: TipoLancamento.Credito,
            valor: 250.75m);
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Act
        var response = await mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Tipo.Should().Be(TipoLancamento.Credito);
        response.Valor.Should().Be(250.75m);

        // Verify database
        var lancamentoInDb = await dbContext.Lancamentos
            .Find(l => l.Id == response.Id)
            .FirstOrDefaultAsync();

        lancamentoInDb.Should().NotBeNull();
        lancamentoInDb!.Tipo.Should().Be(TipoLancamento.Credito);
        lancamentoInDb.IsCredito().Should().BeTrue();
        lancamentoInDb.IsDebito().Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeveSerCriadoComSucesso_QuandoTipoDebito()
    {
        // Arrange
        var command = TestHelpers.CreateValidCriarLancamentoCommand(
            tipo: TipoLancamento.Debito,
            valor: 150.25m);
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        // Act
        var response = await mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Tipo.Should().Be(TipoLancamento.Debito);
        response.Valor.Should().Be(150.25m);

        // Verify database
        var lancamentoInDb = await dbContext.Lancamentos
            .Find(l => l.Id == response.Id)
            .FirstOrDefaultAsync();

        lancamentoInDb.Should().NotBeNull();
        lancamentoInDb!.Tipo.Should().Be(TipoLancamento.Debito);
        lancamentoInDb.IsDebito().Should().BeTrue();
        lancamentoInDb.IsCredito().Should().BeFalse();
    }

}