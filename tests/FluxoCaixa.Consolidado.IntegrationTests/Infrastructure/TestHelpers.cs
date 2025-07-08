using FluxoCaixa.Consolidado.Features.ConsolidarLancamento;
using FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;
using FluxoCaixa.Consolidado.Shared.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.IntegrationTests.Infrastructure;

public static class TestHelpers
{
    public static ConsolidarLancamentoCommand CreateConsolidarLancamentoCommand(
        string? id = null,
        string? comerciante = null,
        decimal? valor = null,
        TipoLancamento? tipo = null,
        DateTime? data = null,
        string? descricao = null)
    {
        return new ConsolidarLancamentoCommand
        {
            LancamentoEvent = new LancamentoEvent
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Comerciante = comerciante ?? "Comerciante Teste",
                Valor = valor ?? 100.50m,
                Tipo = tipo ?? TipoLancamento.Credito,
                Data = data ?? DateTime.UtcNow,
                Descricao = descricao ?? "Descrição teste",
                DataLancamento = DateTime.UtcNow
            }
        };
    }

    public static ConsolidarPeriodoCommand CreateConsolidarPeriodoCommand(
        DateTime? dataInicio = null,
        DateTime? dataFim = null,
        string? comerciante = null)
    {
        return new ConsolidarPeriodoCommand
        {
            DataInicio = dataInicio ?? DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-7), DateTimeKind.Utc),
            DataFim = dataFim ?? DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
            Comerciante = comerciante
        };
    }

    public static LancamentoEvent CreateLancamentoEvent(
        string? id = null,
        string? comerciante = null,
        decimal? valor = null,
        TipoLancamento? tipo = null,
        DateTime? data = null,
        string? descricao = null)
    {
        return new LancamentoEvent
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Comerciante = comerciante ?? "Comerciante Teste",
            Valor = valor ?? 100.50m,
            Tipo = tipo ?? TipoLancamento.Credito,
            Data = data ?? DateTime.UtcNow,
            Descricao = descricao ?? "Descrição teste",
            DataLancamento = DateTime.UtcNow
        };
    }

    public static async Task<Shared.Domain.Entities.Consolidado> CreateConsolidadoInDatabase(
        ConsolidadoTestFactory factory,
        string? comerciante = null,
        DateTime? data = null,
        decimal totalCreditos = 0,
        decimal totalDebitos = 0,
        int quantidadeCreditos = 0,
        int quantidadeDebitos = 0)
    {
        var dbContext = factory.GetDbContext();
        var consolidado = new Shared.Domain.Entities.Consolidado(
            comerciante ?? "Comerciante Teste",
            data ?? DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc)
        );

        // Add some transactions if specified
        for (int i = 0; i < quantidadeCreditos; i++)
        {
            consolidado.AdicionarCredito(totalCreditos / Math.Max(quantidadeCreditos, 1));
        }

        for (int i = 0; i < quantidadeDebitos; i++)
        {
            consolidado.AdicionarDebito(totalDebitos / Math.Max(quantidadeDebitos, 1));
        }

        dbContext.Consolidados.Add(consolidado);
        await dbContext.SaveChangesAsync();
        
        return consolidado;
    }

    public static async Task<List<LancamentoEvent>> CreateMultipleLancamentoEvents(
        int count,
        string? comerciante = null,
        DateTime? baseDate = null,
        TipoLancamento? tipo = null)
    {
        var events = new List<LancamentoEvent>();
        var date = baseDate ?? DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        for (int i = 0; i < count; i++)
        {
            events.Add(CreateLancamentoEvent(
                comerciante: comerciante ?? $"Comerciante {i + 1}",
                valor: (i + 1) * 10.5m,
                tipo: tipo ?? (i % 2 == 0 ? TipoLancamento.Credito : TipoLancamento.Debito),
                data: date.AddDays(i)
            ));
        }

        return events;
    }

    public static async Task ClearDatabase(ConsolidadoTestFactory factory)
    {
        var dbContext = factory.GetDbContext();
        
        dbContext.Consolidados.RemoveRange(dbContext.Consolidados);
        dbContext.Lancamentos.RemoveRange(dbContext.Lancamentos);
        
        await dbContext.SaveChangesAsync();
    }

    public static async Task<Shared.Domain.Entities.Consolidado?> GetConsolidadoFromDatabase(
        ConsolidadoTestFactory factory,
        string comerciante,
        DateTime data)
    {
        var dbContext = factory.GetDbContext();
        var dateOnly = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        return await dbContext.Consolidados
            .FirstOrDefaultAsync(c => c.Comerciante == comerciante && c.Data == dateOnly);
    }

    public static async Task<bool> IsLancamentoConsolidado(
        ConsolidadoTestFactory factory,
        string lancamentoId)
    {
        var dbContext = factory.GetDbContext();
        return await dbContext.Lancamentos
            .AnyAsync(lc => lc.LancamentoId == lancamentoId);
    }

    public static async Task<List<Shared.Domain.Entities.Consolidado>> GetAllConsolidados(ConsolidadoTestFactory factory)
    {
        var dbContext = factory.GetDbContext();
        return await dbContext.Consolidados.ToListAsync();
    }
}