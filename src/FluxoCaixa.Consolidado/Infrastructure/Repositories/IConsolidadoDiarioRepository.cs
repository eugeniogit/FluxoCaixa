using FluxoCaixa.Consolidado.Domain;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public interface IConsolidadoDiarioRepository
{
    Task<Domain.Consolidado?> GetByComercianteAndDataAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default);
    Task<List<Domain.Consolidado>> GetByDataAsync(DateTime data, CancellationToken cancellationToken = default);
    Task<List<Domain.Consolidado>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);
    Task<List<Domain.Consolidado>> GetByComerciante(string comerciante, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Consolidado consolidado, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Consolidado consolidado, CancellationToken cancellationToken = default);
    Task DeleteAsync(Domain.Consolidado consolidado, CancellationToken cancellationToken = default);
    Task DeleteByDataAsync(DateTime data, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Métodos otimizados para high-volume processing
    Task DeleteByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);
    Task DeleteByPeriodoAndComercianteAsync(DateTime dataInicio, DateTime dataFim, string comerciante, CancellationToken cancellationToken = default);
    Task BulkLoadConsolidadosAsync(List<(string Comerciante, DateTime Data)> keys, Dictionary<(string, DateTime), Domain.Consolidado> cache, CancellationToken cancellationToken = default);
}