using FluxoCaixa.Consolidado.Domain;

namespace FluxoCaixa.Consolidado.Infrastructure.Repositories;

public interface ILancamentoProcessadoRepository
{
    Task<bool> JaFoiProcessadoAsync(string lancamentoId, CancellationToken cancellationToken = default);
    Task MarcarComoProcessadoAsync(string lancamentoId, CancellationToken cancellationToken = default);
}