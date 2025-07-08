using FluxoCaixa.Consolidado.Shared.Contracts.Repositories;
using FluxoCaixa.Consolidado.Shared.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Shared.Infrastructure.Repositories;

public class ConsolidadoDiarioRepository : IConsolidadoDiarioRepository
{
    private readonly ConsolidadoDbContext _context;

    public ConsolidadoDiarioRepository(ConsolidadoDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Entities.Consolidado?> GetByComercianteAndDataAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default)
    {
        return await _context.Consolidados
            .FirstOrDefaultAsync(c => c.Comerciante == comerciante && c.Data == data.Date, cancellationToken);
    }

    public async Task<List<Domain.Entities.Consolidado>> GetByDataAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        return await _context.Consolidados
            .Where(c => c.Data == data.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Consolidado>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
    {
        return await _context.Consolidados
            .Where(c => c.Data >= dataInicio.Date && c.Data <= dataFim.Date)
            .OrderBy(c => c.Data)
            .ThenBy(c => c.Comerciante)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Domain.Entities.Consolidado>> GetByComerciante(string comerciante, CancellationToken cancellationToken = default)
    {
        return await _context.Consolidados
            .Where(c => c.Comerciante == comerciante)
            .OrderBy(c => c.Data)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Domain.Entities.Consolidado consolidado, CancellationToken cancellationToken = default)
    {
        await _context.Consolidados.AddAsync(consolidado, cancellationToken);
    }

    public Task UpdateAsync(Domain.Entities.Consolidado consolidado, CancellationToken cancellationToken = default)
    {
        _context.Consolidados.Update(consolidado);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Domain.Entities.Consolidado consolidado, CancellationToken cancellationToken = default)
    {
        _context.Consolidados.Remove(consolidado);
        return Task.CompletedTask;
    }

    public async Task DeleteByDataAsync(DateTime data, CancellationToken cancellationToken = default)
    {
        var consolidacoes = await GetByDataAsync(data, cancellationToken);
        if (consolidacoes.Any())
        {
            _context.Consolidados.RemoveRange(consolidacoes);
        }
    }

    public async Task<bool> ExistsAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default)
    {
        return await _context.Consolidados
            .AnyAsync(c => c.Comerciante == comerciante && c.Data == data.Date, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default)
    {
        await _context.Consolidados
            .Where(c => c.Data >= dataInicio.Date && c.Data <= dataFim.Date)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteByPeriodoAndComercianteAsync(DateTime dataInicio, DateTime dataFim, string comerciante, CancellationToken cancellationToken = default)
    {
        await _context.Consolidados
            .Where(c => c.Data >= dataInicio.Date && c.Data <= dataFim.Date && c.Comerciante == comerciante)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task BulkLoadConsolidadosAsync(List<(string Comerciante, DateTime Data)> keys, Dictionary<(string, DateTime), Domain.Entities.Consolidado> cache, CancellationToken cancellationToken = default)
    {
        if (!keys.Any()) return;

        var keysToLoad = keys.Where(k => !cache.ContainsKey(k)).ToList();
        if (!keysToLoad.Any()) return;

        var dataInicio = keysToLoad.Min(k => k.Data);
        var dataFim = keysToLoad.Max(k => k.Data);
        var comerciantes = keysToLoad.Select(k => k.Comerciante).Distinct().ToList();

        var consolidados = await _context.Consolidados
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