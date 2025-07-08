namespace FluxoCaixa.Consolidado.Shared.Contracts.Repositories;

public interface IConsolidadoDiarioRepository
{
    Task<Domain.Entities.Consolidado?> GetByComercianteAndDataAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.Consolidado>> GetByDataAsync(DateTime data, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.Consolidado>> GetByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);
    Task<List<Domain.Entities.Consolidado>> GetByComerciante(string comerciante, CancellationToken cancellationToken = default);
    Task AddAsync(Domain.Entities.Consolidado consolidado, CancellationToken cancellationToken = default);
    Task UpdateAsync(Domain.Entities.Consolidado consolidado, CancellationToken cancellationToken = default);
    Task DeleteAsync(Domain.Entities.Consolidado consolidado, CancellationToken cancellationToken = default);
    Task DeleteByDataAsync(DateTime data, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string comerciante, DateTime data, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Métodos otimizados para high-volume processing
    Task DeleteByPeriodoAsync(DateTime dataInicio, DateTime dataFim, CancellationToken cancellationToken = default);
    Task DeleteByPeriodoAndComercianteAsync(DateTime dataInicio, DateTime dataFim, string comerciante, CancellationToken cancellationToken = default);
    Task BulkLoadConsolidadosAsync(List<(string Comerciante, DateTime Data)> keys, Dictionary<(string, DateTime), Domain.Entities.Consolidado> cache, CancellationToken cancellationToken = default);
}