using FluxoCaixa.Consolidado.Domain;

namespace FluxoCaixa.Consolidado.Infrastructure.ExternalServices;

public interface ILancamentoApiClient
{
    Task<List<LancamentoEvent>> GetLancamentosByPeriodoAsync(
        DateTime dataInicio, 
        DateTime dataFim, 
        string? comerciante = null,
        bool? consolidado = null);

    Task MarcarLancamentosComoConsolidadosAsync(List<string> lancamentoIds);
}