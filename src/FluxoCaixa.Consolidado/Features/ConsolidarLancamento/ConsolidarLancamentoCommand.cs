using FluxoCaixa.Consolidado.Domain;
using MediatR;

namespace FluxoCaixa.Consolidado.Features.ConsolidarLancamento;

public class ConsolidarLancamentoCommand : IRequest
{
    public LancamentoEvent LancamentoEvent { get; set; } = new();
}