using MediatR;

namespace FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;

public class ConsolidarPeriodoCommand : IRequest
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string? Comerciante { get; set; }
}