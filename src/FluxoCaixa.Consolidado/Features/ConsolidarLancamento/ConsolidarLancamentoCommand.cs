using FluxoCaixa.Consolidado.Shared.Domain.Events;
using MediatR;

namespace FluxoCaixa.Consolidado.Features.ConsolidarLancamento;

public class ConsolidarLancamentoCommand : IRequest
{
    public LancamentoEvent LancamentoEvent { get; set; } = new();
}