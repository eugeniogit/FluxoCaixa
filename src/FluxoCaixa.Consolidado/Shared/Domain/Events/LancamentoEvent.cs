namespace FluxoCaixa.Consolidado.Shared.Domain.Events;

public class LancamentoEvent
{
    public string Id { get; set; } = string.Empty;
    public string Comerciante { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataLancamento { get; set; }

    public bool IsCredito() => Tipo == TipoLancamento.Credito;
    public bool IsDebito() => Tipo == TipoLancamento.Debito;
}

public enum TipoLancamento
{
    Debito = 0,
    Credito = 1
}