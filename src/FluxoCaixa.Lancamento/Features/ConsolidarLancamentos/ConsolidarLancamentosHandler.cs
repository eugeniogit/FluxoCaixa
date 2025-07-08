using FluxoCaixa.Lancamento.Shared.Contracts.Database;
using MediatR;
using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Features.ConsolidarLancamentos;

public class ConsolidarLancamentosHandler : IRequestHandler<ConsolidarLancamentosCommand>
{
    private readonly IDbContext _context;
    private readonly ILogger<ConsolidarLancamentosHandler> _logger;

    public ConsolidarLancamentosHandler(IDbContext context, ILogger<ConsolidarLancamentosHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(ConsolidarLancamentosCommand request, CancellationToken cancellationToken)
    {
        if (!request.LancamentoIds.Any())
        {
            _logger.LogWarning("Nenhum ID de lançamento fornecido para marcar como consolidado");
            return;
        }

        var filter = Builders<Shared.Domain.Entities.Lancamento>.Filter.In(l => l.Id, request.LancamentoIds);
        var update = Builders<Shared.Domain.Entities.Lancamento>.Update.Set(l => l.Consolidado, true);

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