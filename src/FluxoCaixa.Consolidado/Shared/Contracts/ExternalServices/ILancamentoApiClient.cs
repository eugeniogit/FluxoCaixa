using FluxoCaixa.Consolidado.Shared.Domain.Events;

namespace FluxoCaixa.Consolidado.Shared.Contracts.ExternalServices;

public interface ILancamentoApiClient
{
    Task<List<LancamentoEvent>> GetLancamentosByPeriodoAsync(
        DateTime dataInicio, 
        DateTime dataFim, 
        string? comerciante = null,
        bool? consolidado = null);

    Task MarcarLancamentosComoConsolidadosAsync(List<string> lancamentoIds);
}