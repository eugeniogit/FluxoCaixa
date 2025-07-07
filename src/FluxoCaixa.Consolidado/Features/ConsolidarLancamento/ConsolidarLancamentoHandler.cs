using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Infrastructure.Repositories;
using FluxoCaixa.Consolidado.Infrastructure.Messaging;
using MediatR;

namespace FluxoCaixa.Consolidado.Features.ConsolidarLancamento;

public class ConsolidarLancamentoHandler : IRequestHandler<ConsolidarLancamentoCommand>
{
    private readonly IConsolidadoDiarioRepository _repository;
    private readonly ILancamentoConsolidadoRepository _lancamentoConsolidadoRepository;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<ConsolidarLancamentoHandler> _logger;

    public ConsolidarLancamentoHandler(
        IConsolidadoDiarioRepository repository,
        ILancamentoConsolidadoRepository lancamentoProcessadoRepository,
        IRabbitMqPublisher rabbitMqPublisher,
        ILogger<ConsolidarLancamentoHandler> logger)
    {
        _repository = repository;
        _lancamentoConsolidadoRepository = lancamentoProcessadoRepository;
        _rabbitMqPublisher = rabbitMqPublisher;
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

        // Enviar evento para marcar lançamento como consolidado de forma assíncrona
        var marcarConsolidadosEvent = new LancamentosConsolidadosEvent
        {
            LancamentoIds = new List<string> { lancamento.Id }
        };
        
        await _rabbitMqPublisher.PublishLancamentoConsolidadoEventAsync(marcarConsolidadosEvent);

        _logger.LogInformation("Consolidado atualizado para {Comerciante} em {Data}. Saldo: {Saldo}. Evento enviado para marcar lançamento como consolidado.", 
            consolidado.Comerciante, consolidado.Data, consolidado.SaldoLiquido);
    }

    private async Task<Domain.Consolidado> GetOrCreateConsolidado(string comerciante, DateTime data, CancellationToken cancellationToken)
    {
        var consolidado = await _repository.GetByComercianteAndDataAsync(comerciante, data, cancellationToken);

        if (consolidado == null)
        {
            consolidado = new Domain.Consolidado(comerciante, data);
            await _repository.AddAsync(consolidado, cancellationToken);
        }

        return consolidado;
    }
}