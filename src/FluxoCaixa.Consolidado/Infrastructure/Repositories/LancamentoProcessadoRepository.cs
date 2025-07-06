using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public class LancamentoProcessadoRepository : ILancamentoProcessadoRepository
{
    private readonly ConsolidadoDbContext _context;

    public LancamentoProcessadoRepository(ConsolidadoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> JaFoiProcessadoAsync(string lancamentoId, CancellationToken cancellationToken = default)
    {
        return await _context.LancamentosProcessados
            .AnyAsync(lp => lp.LancamentoId == lancamentoId, cancellationToken);
    }

    public async Task MarcarComoProcessadoAsync(string lancamentoId, CancellationToken cancellationToken = default)
    {
        var lancamentoProcessado = new LancamentoProcessado(lancamentoId);
        await _context.LancamentosProcessados.AddAsync(lancamentoProcessado, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}