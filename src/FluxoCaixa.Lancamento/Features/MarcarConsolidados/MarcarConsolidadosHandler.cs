using FluxoCaixa.Lancamento.Infrastructure.Database;
using MediatR;
using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Features.MarcarConsolidados;

public class MarcarConsolidadosHandler : IRequestHandler<MarcarConsolidadosCommand>
{
    private readonly IMongoDbContext _context;
    private readonly ILogger<MarcarConsolidadosHandler> _logger;

    public MarcarConsolidadosHandler(IMongoDbContext context, ILogger<MarcarConsolidadosHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(MarcarConsolidadosCommand request, CancellationToken cancellationToken)
    {
        if (!request.LancamentoIds.Any())
        {
            _logger.LogWarning("Nenhum ID de lançamento fornecido para marcar como consolidado");
            return;
        }

        var filter = Builders<Domain.Lancamento>.Filter.In(l => l.Id, request.LancamentoIds);
        var update = Builders<Domain.Lancamento>.Update.Set(l => l.Consolidado, true);

        var result = await _context.Lancamentos.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);

        _logger.LogInformation("Marcados {Count} lançamentos como consolidados de {Total} solicitados", 
            result.ModifiedCount, request.LancamentoIds.Count);

        if (result.ModifiedCount != request.LancamentoIds.Count)
        {
            _logger.LogWarning("Nem todos os lançamentos foram atualizados. Esperado: {Expected}, Atualizado: {Updated}", 
                request.LancamentoIds.Count, result.ModifiedCount);
        }
    }
}