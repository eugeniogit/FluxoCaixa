using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Infrastructure.Repositories;
using FluxoCaixa.Consolidado.Infrastructure.Messaging;
using MediatR;

namespace FluxoCaixa.Consolidado.Features.ProcessarLancamento;

public class ProcessarLancamentoHandler : IRequestHandler<ProcessarLancamentoCommand>
{
    private readonly IConsolidadoDiarioRepository _repository;
    private readonly ILancamentoProcessadoRepository _lancamentoProcessadoRepository;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<ProcessarLancamentoHandler> _logger;

    public ProcessarLancamentoHandler(
        IConsolidadoDiarioRepository repository,
        ILancamentoProcessadoRepository lancamentoProcessadoRepository,
        IRabbitMqPublisher rabbitMqPublisher,
        ILogger<ProcessarLancamentoHandler> logger)
    {
        _repository = repository;
        _lancamentoProcessadoRepository = lancamentoProcessadoRepository;
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }

    public async Task Handle(ProcessarLancamentoCommand request, CancellationToken cancellationToken)
    {
        var lancamento = request.LancamentoEvent;
        
        // Verificar se o lançamento já foi processado (idempotência)
        var jaProcessado = await _lancamentoProcessadoRepository.JaFoiProcessadoAsync(lancamento.Id, cancellationToken);
        if (jaProcessado)
        {
            _logger.LogInformation("Lançamento {LancamentoId} já foi processado anteriormente. Ignorando duplicata.", lancamento.Id);
            return;
        }

        var dataConsolidacao = DateTime.SpecifyKind(lancamento.Data.Date, DateTimeKind.Utc);

        var consolidado = await GetOrCreateConsolidado(lancamento.Comerciante, dataConsolidacao, cancellationToken);
        
        consolidado.ProcessarLancamento(lancamento);

        // Marcar lançamento como processado para garantir idempotência
        await _lancamentoProcessadoRepository.MarcarComoProcessadoAsync(lancamento.Id, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        // Enviar evento para marcar lançamento como consolidado de forma assíncrona
        var marcarConsolidadosEvent = new MarcarConsolidadosEvent
        {
            LancamentoIds = new List<string> { lancamento.Id }
        };
        
        await _rabbitMqPublisher.PublishMarcarConsolidadosEventAsync(marcarConsolidadosEvent);

        _logger.LogInformation("Consolidado atualizado para {Comerciante} em {Data}. Saldo: {Saldo}. Evento enviado para marcar lançamento como consolidado.", 
            consolidado.Comerciante, consolidado.Data, consolidado.SaldoLiquido);
    }

    private async Task<ConsolidadoDiario> GetOrCreateConsolidado(string comerciante, DateTime data, CancellationToken cancellationToken)
    {
        var consolidado = await _repository.GetByComercianteAndDataAsync(comerciante, data, cancellationToken);

        if (consolidado == null)
        {
            consolidado = new ConsolidadoDiario(comerciante, data);
            await _repository.AddAsync(consolidado, cancellationToken);
        }

        return consolidado;
    }
}