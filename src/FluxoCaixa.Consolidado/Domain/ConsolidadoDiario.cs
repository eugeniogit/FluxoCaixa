using System.ComponentModel.DataAnnotations;

namespace FluxoCaixa.Consolidado.Domain;

public class ConsolidadoDiario
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Comerciante { get; private set; } = string.Empty;

    [Required]
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

    private ConsolidadoDiario() { }

    public ConsolidadoDiario(string comerciante, DateTime data)
    {
        if (string.IsNullOrWhiteSpace(comerciante))
            throw new ArgumentException("Comerciante é obrigatório", nameof(comerciante));
        
        if (data == default)
            throw new ArgumentException("Data é obrigatória", nameof(data));

        Comerciante = comerciante;
        Data = data.Date;
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

    public void ProcessarLancamento(LancamentoEvent lancamento)
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

    public void ProcessarLancamentos(IEnumerable<LancamentoEvent> lancamentos)
    {
        foreach (var lancamento in lancamentos)
        {
            ProcessarLancamento(lancamento);
        }
    }
}