using FluxoCaixa.Consolidado.Shared.Domain.Events;

namespace FluxoCaixa.Consolidado.Shared.Domain.Entities;

public class Consolidado
{
    public int Id { get; set; }

    public string Comerciante { get; private set; } = string.Empty;

    public DateTime Data { get; private set; }

    public decimal TotalCreditos { get; private set; }

    public decimal TotalDebitos { get; private set; }

    private decimal _saldoLiquido;
    public decimal SaldoLiquido 
    { 
        get => _saldoLiquido;
        private set => _saldoLiquido = value;
    }

    public int QuantidadeCreditos { get; private set; }

    public int QuantidadeDebitos { get; private set; }

    public DateTime UltimaAtualizacao { get; private set; } = DateTime.UtcNow;

    private Consolidado() { }

    public Consolidado(string comerciante, DateTime data)
    {
        if (string.IsNullOrWhiteSpace(comerciante))
            throw new ArgumentException("Comerciante é obrigatório", nameof(comerciante));
        
        if (data == default)
            throw new ArgumentException("Data é obrigatória", nameof(data));

        Comerciante = comerciante;
        Data = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        TotalCreditos = 0;
        TotalDebitos = 0;
        QuantidadeCreditos = 0;
        QuantidadeDebitos = 0;
        CalcularSaldoLiquido();
        UltimaAtualizacao = DateTime.UtcNow;
    }

    public void AdicionarCredito(decimal valor)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser positivo", nameof(valor));

        TotalCreditos += valor;
        QuantidadeCreditos++;
        CalcularSaldoLiquido();
        UltimaAtualizacao = DateTime.UtcNow;
    }

    public void AdicionarDebito(decimal valor)
    {
        if (valor <= 0)
            throw new ArgumentException("Valor deve ser positivo", nameof(valor));

        TotalDebitos += valor;
        QuantidadeDebitos++;
        CalcularSaldoLiquido();
        UltimaAtualizacao = DateTime.UtcNow;
    }

    private void CalcularSaldoLiquido()
    {
        SaldoLiquido = TotalCreditos - TotalDebitos;
    }

    public void Consolidar(LancamentoEvent lancamento)
    {
        if (lancamento.IsCredito())
        {
            AdicionarCredito(lancamento.Valor);
        }
        else
        {
            AdicionarDebito(lancamento.Valor);
        }
    }

    public void ConsolidarLancamentos(IEnumerable<LancamentoEvent> lancamentos)
    {
        foreach (var lancamento in lancamentos)
        {
            Consolidar(lancamento);
        }
    }
}