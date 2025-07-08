using FluxoCaixa.Lancamento.Shared.Domain.Entities;
using MediatR;

namespace FluxoCaixa.Lancamento.Features.ListarLancamentos;

public class ListarLancamentosQuery : IRequest<ListarLancamentosResponse>
{
    public string? Comerciante { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public bool Consolidado { get; set; }
}

public class ListarLancamentosResponse
{
    public List<LancamentoDto> Lancamentos { get; set; } = new();
}

public class LancamentoDto
{
    public string Id { get; set; } = string.Empty;
    public string Comerciante { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public TipoLancamento Tipo { get; set; }
    public DateTime Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public DateTime DataLancamento { get; set; }
    public bool Consolidado { get; set; }

    public static LancamentoDto FromLancamento(Shared.Domain.Entities.Lancamento lancamento)
    {
        return new LancamentoDto
        {
            Id = lancamento.Id,
            Comerciante = lancamento.Comerciante,
            Valor = lancamento.Valor,
            Tipo = lancamento.Tipo,
            Data = lancamento.Data,
            Descricao = lancamento.Descricao,
            DataLancamento = lancamento.DataLancamento,
            Consolidado = lancamento.Consolidado
        };
    }
}