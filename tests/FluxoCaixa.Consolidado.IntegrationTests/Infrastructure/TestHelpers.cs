using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Infrastructure.Database;
using FluxoCaixa.Consolidado.Infrastructure.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FluxoCaixa.Consolidado.IntegrationTests.Infrastructure;

public static class TestHelpers
{
    public static DateTime GetTestDate() => DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);

    public static async Task<(IServiceScope scope, ConsolidadoDbContext context, IMediator mediator, IConsolidadoDiarioRepository repository)> 
        CreateTestScope(ConsolidadoTestFactory factory)
    {
        var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var repository = scope.ServiceProvider.GetRequiredService<IConsolidadoDiarioRepository>();
        
        await context.Database.EnsureCreatedAsync();
        
        return (scope, context, mediator, repository);
    }

    public static void AssertConsolidadoValues(
        ConsolidadoDiario? consolidado, 
        decimal expectedCreditos, 
        decimal expectedDebitos, 
        int expectedCreditCount, 
        int expectedDebitCount)
    {
        consolidado.Should().NotBeNull();
        consolidado!.TotalCreditos.Should().Be(expectedCreditos);
        consolidado.TotalDebitos.Should().Be(expectedDebitos);
        consolidado.SaldoLiquido.Should().Be(expectedCreditos - expectedDebitos);
        consolidado.QuantidadeCreditos.Should().Be(expectedCreditCount);
        consolidado.QuantidadeDebitos.Should().Be(expectedDebitCount);
    }

    public static async Task CleanupDatabase(ConsolidadoDbContext context)
    {
        context.ConsolidadosDiarios.RemoveRange(context.ConsolidadosDiarios);
        context.LancamentosProcessados.RemoveRange(context.LancamentosProcessados);
        await context.SaveChangesAsync();
    }

    public static async Task CleanupDatabaseWithRepository(IConsolidadoDiarioRepository repository)
    {
        // Para testes, uma abordagem simples seria deletar todos os registros
        // Em um cenário real, poderiamos ter um método específico no repository
        var today = DateTime.Today;
        var startDate = today.AddDays(-30); // Limpar últimos 30 dias
        var endDate = today.AddDays(30);    // Incluir futuros
        
        var consolidados = await repository.GetByPeriodoAsync(startDate, endDate);
        foreach (var consolidado in consolidados)
        {
            await repository.DeleteAsync(consolidado);
        }
        await repository.SaveChangesAsync();
    }
}