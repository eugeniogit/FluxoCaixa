using FluxoCaixa.Consolidado.Shared.Contracts.Repositories;
using FluxoCaixa.Consolidado.Shared.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Shared.Infrastructure.Repositories;

public class LancamentoConsolidadoRepository : ILancamentoConsolidadoRepository
{
    private readonly ConsolidadoDbContext _context;

    public LancamentoConsolidadoRepository(ConsolidadoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> JaFoiConsolidadoAsync(string lancamentoId, CancellationToken cancellationToken = default)
    {
        return await _context.Lancamentos
            .AnyAsync(lp => lp.LancamentoId == lancamentoId, cancellationToken);
    }

    public async Task ConsolidarAsync(string lancamentoId, CancellationToken cancellationToken = default)
    {
        var lancamentoProcessado = new Domain.Entities.Lancamento(lancamentoId);
        await _context.Lancamentos.AddAsync(lancamentoProcessado, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}