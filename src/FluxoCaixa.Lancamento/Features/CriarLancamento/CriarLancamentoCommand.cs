using FluxoCaixa.Lancamento.Shared.Domain.Entities;
using MediatR;

namespace FluxoCaixa.Lancamento.Features.CriarLancamento;

public class CriarLancamentoCommand : IRequest<CriarLancamentoResponse>
{
    public string Comerciante { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
}

public class CriarLancamentoResponse
{
    public string Id { get; set; } = string.Empty;
    public string Comerciante { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataLancamento { get; set; }

    public static CriarLancamentoResponse FromLancamento(Shared.Domain.Entities.Lancamento lancamento)
    {
        return new CriarLancamentoResponse
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