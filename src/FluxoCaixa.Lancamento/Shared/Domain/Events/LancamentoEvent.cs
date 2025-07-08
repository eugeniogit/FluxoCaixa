using FluxoCaixa.Lancamento.Shared.Domain.Entities;

namespace FluxoCaixa.Lancamento.Shared.Domain.Events;

public class LancamentoEvent
{
    public string Id { get; private set; } = string.Empty;
    public string Comerciante { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public DateTime Data { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public DateTime DataLancamento { get; private set; }

    private LancamentoEvent() { }

    public static LancamentoEvent FromLancamento(Entities.Lancamento lancamento)
    {
        return new LancamentoEvent
        {
            Id = lancamento.Id,
            Comerciante = lancamento.Comerciante,
            Valor = lancamento.Valor,
            Tipo = lancamento.Tipo,
            Data = lancamento.Data,
            Descricao = lancamento.Descricao,
            DataLancamento = lancamento.DataLancamento
        };
    }
}