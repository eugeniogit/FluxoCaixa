using MediatR;

namespace FluxoCaixa.Lancamento.Features.MarcarConsolidados;

public class MarcarConsolidadosCommand : IRequest
{
    public List<string> LancamentoIds { get; set; } = new();
}

public class MarcarConsolidadosRequest
{
    public List<string> LancamentoIds { get; set; } = new();
}