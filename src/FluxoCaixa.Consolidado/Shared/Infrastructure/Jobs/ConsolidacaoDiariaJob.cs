using FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;
using MediatR;
using Quartz;

namespace FluxoCaixa.Consolidado.Shared.Infrastructure.Jobs;

public class ConsolidacaoDiariaJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsolidacaoDiariaJob> _logger;

    public ConsolidacaoDiariaJob(IServiceProvider serviceProvider, ILogger<ConsolidacaoDiariaJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Iniciando job de consolidação diária");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var dataConsolidacao = DateTime.Today.AddDays(-1);

            var command = new ConsolidarPeriodoCommand
            {
                DataInicio = dataConsolidacao,
                DataFim = dataConsolidacao
            };

            await mediator.Send(command);

            _logger.LogInformation("Job de consolidação diária executado com sucesso para data {Data}", dataConsolidacao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar job de consolidação diária");
            throw;
        }
    }

}