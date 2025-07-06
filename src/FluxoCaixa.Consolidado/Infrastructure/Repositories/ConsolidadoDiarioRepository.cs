using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public class ConsolidadoDiarioRepository : IConsolidadoDiarioRepository
{
    private readonly ConsolidadoDbContext _context;

    public ConsolidadoDiarioRepository(ConsolidadoDbContext context)
    {
        _context = context;
    }

    public async Task<ConsolidadoDiario?> GetByComercianteAndDataAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default)
    {
        return await _context.ConsolidadosDiarios
            .FirstOrDefaultAsync(c => c.Comerciante == comerciante && c.Data == data.Date, cancellationToken);
    }

    public async Task<List<ConsolidadoDiario>> GetByDataAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        return await _context.ConsolidadosDiarios
            .Where(c => c.Data == data.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConsolidadoDiario>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
    {
        return await _context.ConsolidadosDiarios
            .Where(c => c.Data >= dataInicio.Date && c.Data <= dataFim.Date)
            .OrderBy(c => c.Data)
            .ThenBy(c => c.Comerciante)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ConsolidadoDiario>> GetByComerciante(string comerciante, CancellationToken cancellationToken = default)
    {
        return await _context.ConsolidadosDiarios
            .Where(c => c.Comerciante == comerciante)
            .OrderBy(c => c.Data)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default)
    {
        await _context.ConsolidadosDiarios.AddAsync(consolidado, cancellationToken);
    }

    public Task UpdateAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default)
    {
        _context.ConsolidadosDiarios.Update(consolidado);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default)
    {
        _context.ConsolidadosDiarios.Remove(consolidado);
        return Task.CompletedTask;
    }

    public async Task DeleteByDataAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        var consolidacoes = await GetByDataAsync(data, cancellationToken);
        if (consolidacoes.Any())
        {
            _context.ConsolidadosDiarios.RemoveRange(consolidacoes);
        }
    }

    public async Task<bool> ExistsAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default)
    {
        return await _context.ConsolidadosDiarios
            .AnyAsync(c => c.Comerciante == comerciante && c.Data == data.Date, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
    {
        await _context.ConsolidadosDiarios
            .Where(c => c.Data >= dataInicio.Date && c.Data <= dataFim.Date)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteByPeriodoAndComercianteAsync(DateTime dataInicio, DateTime dataFim, string comerciante, CancellationToken cancellationToken = default)
    {
        await _context.ConsolidadosDiarios
            .Where(c => c.Data >= dataInicio.Date && c.Data <= dataFim.Date && c.Comerciante == comerciante)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task BulkLoadConsolidadosAsync(List<(string Comerciante, DateTime Data)> keys, Dictionary<(string, DateTime), ConsolidadoDiario> cache, CancellationToken cancellationToken = default)
    {
        if (!keys.Any()) return;

        var keysToLoad = keys.Where(k => !cache.ContainsKey(k)).ToList();
        if (!keysToLoad.Any()) return;

        var dataInicio = keysToLoad.Min(k => k.Data);
        var dataFim = keysToLoad.Max(k => k.Data);
        var comerciantes = keysToLoad.Select(k => k.Comerciante).Distinct().ToList();

        var consolidados = await _context.ConsolidadosDiarios
            .Where(c => c.Data >= dataInicio && c.Data <= dataFim && comerciantes.Contains(c.Comerciante))
            .ToListAsync(cancellationToken);

        foreach (var consolidado in consolidados)
        {
            var key = (consolidado.Comerciante, consolidado.Data);
            if (!cache.ContainsKey(key))
            {
                cache[key] = consolidado;
            }
        }
    }
}