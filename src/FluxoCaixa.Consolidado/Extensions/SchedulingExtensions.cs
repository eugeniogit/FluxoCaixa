using FluxoCaixa.Consolidado.Shared.Configurations;
using FluxoCaixa.Consolidado.Shared.Infrastructure.Jobs;
using Quartz;

namespace FluxoCaixa.Consolidado.Extensions;

public static class SchedulingExtensions
{
    public static IServiceCollection AddQuartzScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("ConsolidacaoDiariaJob");
            q.AddJob<ConsolidacaoDiariaJob>(opts => opts.WithIdentity(jobKey));
            
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("ConsolidacaoDiariaJob-trigger")
                .WithCronSchedule(Constants.Scheduling.DailyConsolidationCron));
        });
        
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        
        return services;
    }
}