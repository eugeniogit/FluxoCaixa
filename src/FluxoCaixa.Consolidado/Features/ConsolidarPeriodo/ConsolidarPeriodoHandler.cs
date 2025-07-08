using FluxoCaixa.Consolidado.Shared.Contracts.ExternalServices;
using FluxoCaixa.Consolidado.Shared.Contracts.Messaging;
using FluxoCaixa.Consolidado.Shared.Contracts.Repositories;
using FluxoCaixa.Consolidado.Shared.Domain.Events;
using MediatR;

namespace FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;

public class ConsolidarPeriodoHandler : IRequestHandler<ConsolidarPeriodoCommand>
{
    private readonly IConsolidadoDiarioRepository _repository;
    private readonly ILancamentoApiClient _lancamentoApiClient;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<ConsolidarPeriodoHandler> _logger;

    public ConsolidarPeriodoHandler(
        IConsolidadoDiarioRepository repository,
        ILancamentoApiClient lancamentoApiClient,
        IMessagePublisher messagePublisher,
        ILogger<ConsolidarPeriodoHandler> logger)
    {
        _repository = repository;
        _lancamentoApiClient = lancamentoApiClient;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task Handle(ConsolidarPeriodoCommand request, CancellationToken cancellationToken)
    {
        var dataInicio = request.DataInicio.Date;
        var dataFim = request.DataFim.Date;
        LogConsolidationStart(dataInicio, dataFim, request.Comerciante);

        try
        {
            await ProcessLancamentosNaoConsolidados(dataInicio, dataFim, request.Comerciante, cancellationToken);

            _logger.LogInformation("Consolidação concluída para período {DataInicio} até {DataFim}", dataInicio, dataFim);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante consolidação do período {DataInicio} até {DataFim}", dataInicio, dataFim);
            throw;
        }
    }


    private async Task ProcessLancamentosNaoConsolidados(DateTime dataInicio, DateTime dataFim, string? comerciante, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Buscando lançamentos não consolidados para período {DataInicio} até {DataFim}", dataInicio, dataFim);

        var lancamentos = await _lancamentoApiClient.GetLancamentosByPeriodoAsync(
            dataInicio, dataFim, comerciante, consolidado: false);

        if (!lancamentos.Any())
        {
            _logger.LogInformation("Nenhum lançamento não consolidado encontrado para o período");
            return;
        }

        _logger.LogInformation("Processando {Count} lançamentos não consolidados", lancamentos.Count);

        var grupos = lancamentos.GroupBy(l => (l.Comerciante, l.Data.Date));

        foreach (var grupo in grupos)
        {
            await ProcessMerchantDateGroup(grupo, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var lancamentoIds = lancamentos.Select(l => l.Id).ToList();
        var marcarConsolidadosEvent = new LancamentosConsolidadosEvent
        {
            LancamentoIds = lancamentoIds
        };
        
        await _messagePublisher.PublishAsync(marcarConsolidadosEvent, "marcar_consolidados_events");

        _logger.LogInformation("Processamento concluído. Total de lançamentos processados: {Total}. Evento enviado para marcar como consolidados.", lancamentos.Count);
    }

    private async Task ProcessMerchantDateGroup(
        IGrouping<(string Comerciante, DateTime Data), LancamentoEvent> grupo,
        CancellationToken cancellationToken)
    {
        var key = grupo.Key;
        var lancamentosComerciante = grupo.ToList();

        var consolidado = await _repository.GetByComercianteAndDataAsync(key.Comerciante, key.Data, cancellationToken);
        if (consolidado == null)
        {
            consolidado = new Shared.Domain.Entities.Consolidado(key.Comerciante, key.Data);
            await _repository.AddAsync(consolidado, cancellationToken);
        }

        consolidado.ConsolidarLancamentos(lancamentosComerciante);
        
        LogConsolidationResult(consolidado);
    }


    private void LogConsolidationStart(DateTime dataInicio, DateTime dataFim, string? comerciante)
    {
        _logger.LogInformation("Iniciando consolidação para período {DataInicio} até {DataFim} e comerciante {Comerciante}", 
            dataInicio, dataFim, comerciante);
    }


    private void LogConsolidationResult(Shared.Domain.Entities.Consolidado consolidado)
    {
        _logger.LogInformation("Consolidado atualizado para {Comerciante} em {Data}. " +
            "Créditos: {Creditos}, Débitos: {Debitos}, Saldo: {Saldo}", 
            consolidado.Comerciante, consolidado.Data, consolidado.TotalCreditos, 
            consolidado.TotalDebitos, consolidado.SaldoLiquido);
    }
}