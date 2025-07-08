using FluentAssertions;
using FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;
using FluxoCaixa.Lancamento.Shared.Domain.Entities;
using MongoDB.Driver;
using Xunit;

namespace FluxoCaixa.Lancamento.IntegrationTests.Features;

[Collection("LancamentoIntegrationTests")]
public class SimpleDbTest : IClassFixture<LancamentoTestFactory>
{
    private readonly LancamentoTestFactory _factory;

    public SimpleDbTest(LancamentoTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_ShouldWork_WhenDirectlyInsertingData()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var dbContext = _factory.GetDbContext();
        
        var lancamento = new Shared.Domain.Entities.Lancamento(
            "Test Comerciante",
            100m,
            TipoLancamento.Credito,
            DateTime.UtcNow,
            "Test Description"
        );

        // Act
        await dbContext.Lancamentos.InsertOneAsync(lancamento);

        // Assert
        var count = await dbContext.Lancamentos.CountDocumentsAsync(_ => true);
        count.Should().Be(1);

        var retrieved = await dbContext.Lancamentos
            .Find(l => l.Id == lancamento.Id)
            .FirstOrDefaultAsync();

        retrieved.Should().NotBeNull();
        retrieved!.Comerciante.Should().Be("Test Comerciante");
    }
}