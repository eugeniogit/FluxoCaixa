namespace FluxoCaixa.Lancamento.Shared.Domain.Events;

public class LancamentosConsolidadosEvent
{
	public List<string> LancamentoIds { get; set; } = new();
	public DateTime DataProcessamento { get; set; } = DateTime.UtcNow;
}