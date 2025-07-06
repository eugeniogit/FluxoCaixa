using FluxoCaixa.Consolidado.Domain;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public interface IConsolidadoDiarioRepository
{
    Task<ConsolidadoDiario?> GetByComercianteAndDataAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default);
    Task<List<ConsolidadoDiario>> GetByDataAsync(DateTime data, CancellationToken cancellationToken = default);
    Task<List<ConsolidadoDiario>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);
    Task<List<ConsolidadoDiario>> GetByComerciante(string comerciante, CancellationToken cancellationToken = default);
    Task AddAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default);
    Task UpdateAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default);
    Task DeleteAsync(ConsolidadoDiario consolidado, CancellationToken cancellationToken = default);
    Task DeleteByDataAsync(DateTime data, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Métodos otimizados para high-volume processing
    Task DeleteByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);
    Task DeleteByPeriodoAndComercianteAsync(DateTime dataInicio, DateTime dataFim, string comerciante, CancellationToken cancellationToken = default);
    Task BulkLoadConsolidadosAsync(List<(string Comerciante, DateTime Data)> keys, Dictionary<(string, DateTime), ConsolidadoDiario> cache, CancellationToken cancellationToken = default);
}