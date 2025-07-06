using FluxoCaixa.Consolidado.Domain;
using MediatR;

namespace FluxoCaixa.Consolidado.Features.ProcessarLancamento;

public class ProcessarLancamentoCommand : IRequest
{
    public LancamentoEvent LancamentoEvent { get; set; } = new();
}