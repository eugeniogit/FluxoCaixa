using FluxoCaixa.Lancamento.Domain;
using FluxoCaixa.Lancamento.Infrastructure.Database;
using FluxoCaixa.Lancamento.Infrastructure.Messaging;
using MediatR;

namespace FluxoCaixa.Lancamento.Features.CriarLancamento;

public class CriarLancamentoHandler : IRequestHandler<CriarLancamentoCommand, CriarLancamentoResponse>
{
    private readonly IMongoDbContext _context;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<CriarLancamentoHandler> _logger;

    public CriarLancamentoHandler(
        IMongoDbContext context, 
        IRabbitMqPublisher publisher,
        ILogger<CriarLancamentoHandler> logger)
    {
        _context = context;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CriarLancamentoResponse> Handle(CriarLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = new Domain.Lancamento(
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

    private async Task PublishEventAsync(Domain.Lancamento lancamento)
    {
        try
        {
            var lancamentoEvent = LancamentoEvent.FromLancamento(lancamento);
            await _publisher.PublishLancamentoEventAsync(lancamentoEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento de lançamento: {LancamentoId}", lancamento.Id);
        }
    }
}