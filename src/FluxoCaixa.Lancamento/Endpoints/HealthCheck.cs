namespace FluxoCaixa.Lancamento.Endpoints;

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
                service = "FluxoCaixa.Lancamento"
            });
        })
        .WithName("HealthCheck")
        .WithSummary("Verificar saúde do serviço")
        .WithDescription("Retorna o status de saúde do serviço")
        .WithTags("Health")
        .AllowAnonymous();

        app.MapGet("/health/ready", () =>
        {
            return Results.Ok(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow,
                service = "FluxoCaixa.Lancamento"
            });
        })
        .WithName("ReadinessCheck")
        .WithSummary("Verificar se o serviço está pronto")
        .WithDescription("Retorna se o serviço está pronto para receber requisições")
        .WithTags("Health")
        .AllowAnonymous();
    }
}