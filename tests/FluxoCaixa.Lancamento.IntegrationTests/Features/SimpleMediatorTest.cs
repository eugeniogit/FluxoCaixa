using FluentAssertions;
using FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;
using MediatR;
using MongoDB.Driver;
using Xunit;

namespace FluxoCaixa.Lancamento.IntegrationTests.Features;

[Collection("LancamentoIntegrationTests")]
public class SimpleMediatorTest : IClassFixture<LancamentoTestFactory>
{
    private readonly LancamentoTestFactory _factory;

    public SimpleMediatorTest(LancamentoTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Mediator_ShouldProcessCommand_WhenValidCommand()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var mediator = _factory.GetMediator();
        var dbContext = _factory.GetDbContext();

        var command = TestHelpers.CreateValidCriarLancamentoCommand(
            comerciante: "Single Test Comerciante",
            valor: 99.99m
        );

        // Act
        var response = await mediator.Send(command);

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeNullOrEmpty();
        response.Comerciante.Should().Be("Single Test Comerciante");
        response.Valor.Should().Be(99.99m);

        // Check database immediately
        var dbRecord = await dbContext.Lancamentos
            .Find(l => l.Id == response.Id)
            .FirstOrDefaultAsync();

        dbRecord.Should().NotBeNull("Record should exist in database");
        dbRecord!.Comerciante.Should().Be("Single Test Comerciante");
        dbRecord.Valor.Should().Be(99.99m);
    }
}