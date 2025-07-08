using FluxoCaixa.Consolidado.Shared.Contracts.ExternalServices;

namespace FluxoCaixa.Consolidado.Entdpoints;

public static class Test
{
    public static void MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/test").WithTags("Test");

        group.MapGet("/auth", async (ILancamentoApiClient lancamentoApiClient) =>
        {
            try
            {
                // Test authentication with Lancamento API
                var testDate = DateTime.Today;
                await lancamentoApiClient.GetLancamentosByPeriodoAsync(testDate, testDate);
                
                return Results.Ok(new { 
                    status = "authenticated", 
                    message = "Successfully authenticated with Lancamento API",
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    title: "Error testing Lancamento API authentication",
                    statusCode: 500);
            }
        })
        .WithName("TestLancamentoApiAuth")
        .WithSummary("Test authentication with Lancamento API")
        .WithDescription("Tests if the Consolidado service can authenticate with the Lancamento API");
    }
}