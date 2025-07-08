using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FluxoCaixa.Consolidado.Entdpoints;

public static class HealthCheck
{
    public static void MapHealthCheckEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "FluxoCaixa.Consolidado"
            });
        })
        .WithName("HealthCheck")
        .WithSummary("Verificar saúde do serviço")
        .WithDescription("Retorna o status de saúde do serviço")
        .WithTags("Health");

        app.MapGet("/health/ready", async (HealthCheckService healthCheckService) =>
        {
            try
            {
                var healthReport = await healthCheckService.CheckHealthAsync();
                
                var response = new
                {
                    status = healthReport.Status == HealthStatus.Healthy ? "ready" : "not ready",
                    timestamp = DateTime.UtcNow,
                    service = "FluxoCaixa.Consolidado",
                    checks = healthReport.Entries.ToDictionary(
                        entry => entry.Key,
                        entry => new
                        {
                            status = entry.Value.Status.ToString().ToLower(),
                            description = entry.Value.Description,
                            duration = entry.Value.Duration.TotalMilliseconds
                        })
                };

                return healthReport.Status == HealthStatus.Healthy 
                    ? Results.Ok(response)
                    : Results.StatusCode(503);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    title: "Service not ready",
                    statusCode: 503);
            }
        })
        .WithName("ReadinessCheck")
        .WithSummary("Verificar prontidão do serviço")
        .WithDescription("Retorna o status de prontidão do serviço incluindo dependências")
        .WithTags("Health");
    }
}