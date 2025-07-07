using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public class LancamentoProcessadoRepository : ILancamentoConsolidadoRepository
{
    private readonly ConsolidadoDbContext _context;

    public LancamentoProcessadoRepository(ConsolidadoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> JaFoiConsolidadoAsync(string lancamentoId, CancellationToken cancellationToken = default)
    {
        return await _context.LancamentosProcessados
            .AnyAsync(lp => lp.LancamentoId == lancamentoId, cancellationToken);
    }

    public async Task ConsolidarAsync(string lancamentoId, CancellationToken cancellationToken = default)
    {
        var lancamentoProcessado = new LancamentoConsolidado(lancamentoId);
        await _context.LancamentosProcessados.AddAsync(lancamentoProcessado, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}