using MediatR;
using FluxoCaixa.Consolidado.Shared.Contracts.Repositories;
using FluxoCaixa.Consolidado.Shared.Contracts.Messaging;
using FluxoCaixa.Consolidado.Shared.Domain.Events;

namespace FluxoCaixa.Consolidado.Features.ConsolidarLancamento;

public class ConsolidarLancamentoHandler : IRequestHandler<ConsolidarLancamentoCommand>
{
    private readonly IConsolidadoDiarioRepository _repository;
    private readonly ILancamentoConsolidadoRepository _lancamentoConsolidadoRepository;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<ConsolidarLancamentoHandler> _logger;

    public ConsolidarLancamentoHandler(
        IConsolidadoDiarioRepository repository,
        ILancamentoConsolidadoRepository lancamentoProcessadoRepository,
        IMessagePublisher messagePublisher,
        ILogger<ConsolidarLancamentoHandler> logger)
    {
        _repository = repository;
        _lancamentoConsolidadoRepository = lancamentoProcessadoRepository;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task Handle(ConsolidarLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = request.LancamentoEvent;
        
        var jaConsolidado = await _lancamentoConsolidadoRepository.JaFoiConsolidadoAsync(lancamento.Id, cancellationToken);
        if (jaConsolidado)
        {
            _logger.LogInformation("Lançamento {LancamentoId} já foi processado anteriormente. Ignorando duplicata.", lancamento.Id);
            return;
        }

        var dataConsolidacao = DateTime.SpecifyKind(lancamento.Data.Date, DateTimeKind.Utc);

        var consolidado = await GetOrCreateConsolidado(lancamento.Comerciante, dataConsolidacao, cancellationToken);
        
        consolidado.Consolidar(lancamento);

        // Marcar lançamento como processado para garantir idempotência
        await _lancamentoConsolidadoRepository.ConsolidarAsync(lancamento.Id, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        var marcarConsolidadosEvent = new LancamentosConsolidadosEvent
        {
            LancamentoIds = new List<string> { lancamento.Id }
        };
        
        await _messagePublisher.PublishAsync(marcarConsolidadosEvent, "marcar_consolidados_events");

        _logger.LogInformation("Consolidado atualizado para {Comerciante} em {Data}. Saldo: {Saldo}. Evento enviado para marcar lançamento como consolidado.", 
            consolidado.Comerciante, consolidado.Data, consolidado.SaldoLiquido);
    }

    private async Task<Shared.Domain.Entities.Consolidado> GetOrCreateConsolidado(string comerciante, DateTime data, CancellationToken cancellationToken)
    {
        var consolidado = await _repository.GetByComercianteAndDataAsync(comerciante, data, cancellationToken);

        if (consolidado == null)
        {
            consolidado = new Shared.Domain.Entities.Consolidado(comerciante, data);
            await _repository.AddAsync(consolidado, cancellationToken);
        }

        return consolidado;
    }
}