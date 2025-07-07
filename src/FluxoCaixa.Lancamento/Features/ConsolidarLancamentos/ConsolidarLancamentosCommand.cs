using MediatR;

namespace FluxoCaixa.Lancamento.Features.ConsolidarLancamentos;

public class ConsolidarLancamentosCommand : IRequest
{
    public List<string> LancamentoIds { get; set; } = new();
}