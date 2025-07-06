using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Infrastructure.ExternalServices;
using FluxoCaixa.Consolidado.Infrastructure.Messaging;
using FluxoCaixa.Consolidado.Infrastructure.Repositories;
using MediatR;

namespace FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;

public class ConsolidarPeriodoHandler : IRequestHandler<ConsolidarPeriodoCommand>
{
    private readonly IConsolidadoDiarioRepository _repository;
    private readonly ILancamentoApiClient _lancamentoApiClient;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<ConsolidarPeriodoHandler> _logger;

    public ConsolidarPeriodoHandler(
        IConsolidadoDiarioRepository repository,
        ILancamentoApiClient lancamentoApiClient,
        IRabbitMqPublisher rabbitMqPublisher,
        ILogger<ConsolidarPeriodoHandler> logger)
    {
        _repository = repository;
        _lancamentoApiClient = lancamentoApiClient;
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }

    public async Task Handle(ConsolidarPeriodoCommand request, CancellationToken cancellationToken)
    {
        var dataInicio = request.DataInicio.Date;
        var dataFim = request.DataFim.Date;
        LogConsolidationStart(dataInicio, dataFim, request.Comerciante);

        try
        {
            // Buscar apenas lançamentos não consolidados
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

        // Buscar apenas lançamentos não consolidados
        var lancamentos = await _lancamentoApiClient.GetLancamentosByPeriodoAsync(
            dataInicio, dataFim, comerciante, consolidado: false);

        if (!lancamentos.Any())
        {
            _logger.LogInformation("Nenhum lançamento não consolidado encontrado para o período");
            return;
        }

        _logger.LogInformation("Processando {Count} lançamentos não consolidados", lancamentos.Count);

        // Agrupar lançamentos por comerciante/data
        var grupos = lancamentos.GroupBy(l => (l.Comerciante, l.Data.Date));

        // Processar cada grupo
        foreach (var grupo in grupos)
        {
            await ProcessMerchantDateGroup(grupo, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        // Enviar evento para marcar lançamentos como consolidados de forma assíncrona
        var lancamentoIds = lancamentos.Select(l => l.Id).ToList();
        var marcarConsolidadosEvent = new Infrastructure.Messaging.MarcarConsolidadosEvent
        {
            LancamentoIds = lancamentoIds
        };
        
        await _rabbitMqPublisher.PublishMarcarConsolidadosEventAsync(marcarConsolidadosEvent);

        _logger.LogInformation("Processamento concluído. Total de lançamentos processados: {Total}. Evento enviado para marcar como consolidados.", lancamentos.Count);
    }

    private async Task ProcessMerchantDateGroup(
        IGrouping<(string Comerciante, DateTime Data), LancamentoEvent> grupo,
        CancellationToken cancellationToken)
    {
        var key = grupo.Key;
        var lancamentosComerciante = grupo.ToList();

        // Buscar consolidado existente ou criar novo
        var consolidado = await _repository.GetByComercianteAndDataAsync(key.Comerciante, key.Data, cancellationToken);
        if (consolidado == null)
        {
            consolidado = new ConsolidadoDiario(key.Comerciante, key.Data);
            await _repository.AddAsync(consolidado, cancellationToken);
        }

        // Processar lançamentos
        consolidado.ProcessarLancamentos(lancamentosComerciante);
        
        LogConsolidationResult(consolidado);
    }


    private void LogConsolidationStart(DateTime dataInicio, DateTime dataFim, string? comerciante)
    {
        _logger.LogInformation("Iniciando consolidação para período {DataInicio} até {DataFim} e comerciante {Comerciante}", 
            dataInicio, dataFim, comerciante);
    }


    private void LogConsolidationResult(ConsolidadoDiario consolidado)
    {
        _logger.LogInformation("Consolidado atualizado para {Comerciante} em {Data}. " +
            "Créditos: {Creditos}, Débitos: {Debitos}, Saldo: {Saldo}", 
            consolidado.Comerciante, consolidado.Data, consolidado.TotalCreditos, 
            consolidado.TotalDebitos, consolidado.SaldoLiquido);
    }
}