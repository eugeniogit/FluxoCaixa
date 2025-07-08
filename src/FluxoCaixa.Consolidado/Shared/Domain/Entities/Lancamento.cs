namespace FluxoCaixa.Consolidado.Shared.Domain.Entities;

public class Lancamento
{
    private Lancamento()
    {
        
    }

    public Lancamento(string lancamentoId)
    {
        ValidateInput(lancamentoId);
        
        LancamentoId = lancamentoId;
        DataProcessamento = DateTime.UtcNow;
    }

    public string LancamentoId { get; private set; } = string.Empty;
    public DateTime DataProcessamento { get; private set; }

    private static void ValidateInput(string lancamentoId)
    {
        if (string.IsNullOrWhiteSpace(lancamentoId))
            throw new ArgumentException("ID do lançamento é obrigatório", nameof(lancamentoId));
    }
}