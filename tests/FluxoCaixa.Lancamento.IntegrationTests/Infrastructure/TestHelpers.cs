using FluxoCaixa.Lancamento.Features.ConsolidarLancamentos;
using FluxoCaixa.Lancamento.Features.CriarLancamento;
using FluxoCaixa.Lancamento.Shared.Domain.Entities;

namespace FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;

public static class TestHelpers
{
    public static CriarLancamentoCommand CreateValidCriarLancamentoCommand(
        string? comerciante = null,
        decimal? valor = null,
        TipoLancamento? tipo = null,
        DateTime? data = null,
        string? descricao = null)
    {
        return new CriarLancamentoCommand
        {
            Comerciante = comerciante ?? "Comerciante Teste",
            Valor = valor ?? 100.50m,
            Tipo = tipo ?? TipoLancamento.Credito,
            Data = data ?? DateTime.UtcNow,
            Descricao = descricao ?? "Descrição teste"
        };
    }

    public static ConsolidarLancamentosCommand CreateMarcarConsolidadosCommand(params string[] lancamentoIds)
    {
        return new ConsolidarLancamentosCommand
        {
            LancamentoIds = lancamentoIds.ToList()
        };
    }

    public static async Task<Shared.Domain.Entities.Lancamento> CreateLancamentoInDatabase(
        LancamentoTestFactory factory,
        string? comerciante = null,
        decimal? valor = null,
        TipoLancamento? tipo = null,
        DateTime? data = null,
        string? descricao = null)
    {
        var dbContext = factory.GetDbContext();
        var lancamento = new Shared.Domain.Entities.Lancamento(
            comerciante ?? "Comerciante Teste",
            valor ?? 100.50m,
            tipo ?? TipoLancamento.Credito,
            data ?? DateTime.UtcNow,
            descricao ?? "Descrição teste"
        );

        await dbContext.Lancamentos.InsertOneAsync(lancamento);
        return lancamento;
    }

    public static async Task<List<Shared.Domain.Entities.Lancamento>> CreateMultipleLancamentosInDatabase(
        LancamentoTestFactory factory,
        int count,
        string? comerciante = null,
        TipoLancamento? tipo = null)
    {
        var lancamentos = new List<Shared.Domain.Entities.Lancamento>();
        var dbContext = factory.GetDbContext();

        for (int i = 0; i < count; i++)
        {
            var lancamento = new Shared.Domain.Entities.Lancamento(
                comerciante ?? $"Comerciante {i + 1}",
                (i + 1) * 10.5m,
                tipo ?? (i % 2 == 0 ? TipoLancamento.Credito : TipoLancamento.Debito),
                DateTime.UtcNow.AddDays(-i),
                $"Descrição {i + 1}"
            );

            await dbContext.Lancamentos.InsertOneAsync(lancamento);
            lancamentos.Add(lancamento);
        }

        return lancamentos;
    }
}