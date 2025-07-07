using FluxoCaixa.Consolidado.Domain;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public interface ILancamentoConsolidadoRepository
{
    Task<bool> JaFoiConsolidadoAsync(string lancamentoId, CancellationToken cancellationToken = default);
    Task ConsolidarAsync(string lancamentoId, CancellationToken cancellationToken = default);
}