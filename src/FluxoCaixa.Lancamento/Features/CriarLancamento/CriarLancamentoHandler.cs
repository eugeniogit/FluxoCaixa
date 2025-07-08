using FluxoCaixa.Lancamento.Shared.Contracts.Database;
using FluxoCaixa.Lancamento.Shared.Contracts.Messaging;
using FluxoCaixa.Lancamento.Shared.Domain.Events;
using MediatR;

namespace FluxoCaixa.Lancamento.Features.CriarLancamento;

public class CriarLancamentoHandler : IRequestHandler<CriarLancamentoCommand, CriarLancamentoResponse>
{
    private readonly IDbContext _context;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<CriarLancamentoHandler> _logger;

    public CriarLancamentoHandler(
        IDbContext context, 
        IMessagePublisher publisher,
        ILogger<CriarLancamentoHandler> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CriarLancamentoResponse> Handle(CriarLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = new Shared.Domain.Entities.Lancamento(
            request.Comerciante,
            request.Valor,
            request.Tipo,
            request.Data,
            request.Descricao);

        await _context.Lancamentos.InsertOneAsync(lancamento, cancellationToken: cancellationToken);
        _logger.LogInformation("Lançamento criado: {LancamentoId}", lancamento.Id);

        await PublishEventAsync(lancamento);

        return CriarLancamentoResponse.FromLancamento(lancamento);
    }

    private async Task PublishEventAsync(Shared.Domain.Entities.Lancamento lancamento)
    {
        try
        {
            var lancamentoEvent = LancamentoEvent.FromLancamento(lancamento);
            await _publisher.PublishAsync(lancamentoEvent, "lancamento_events");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento de lançamento: {LancamentoId}", lancamento.Id);
        }
    }
}