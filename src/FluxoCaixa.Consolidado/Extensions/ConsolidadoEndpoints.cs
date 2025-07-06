using FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace FluxoCaixa.Consolidado.Extensions;

public static class ConsolidadoEndpoints
{
    public static void MapConsolidadoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/consolidado").WithTags("Consolidação");

        group.MapPost("/consolidar", async (ConsolidarPeriodoRequest request, IMediator mediator) =>
        {
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));
            }

            var command = new ConsolidarPeriodoCommand
            {
                DataInicio = request.DataInicio,
                DataFim = request.DataFim,
                Comerciante = request.Comerciante
            };

            await mediator.Send(command);

            return Results.Ok(new { 
                message = "Consolidação executada com sucesso", 
                dataInicio = request.DataInicio,
                dataFim = request.DataFim,
                comerciante = request.Comerciante 
            });
        })
        .WithName("ConsolidarPeriodo")
        .WithSummary("Executar consolidação de período")
        .WithDescription("Executa a consolidação manual dos lançamentos para um período específico (data início e fim)");

        group.MapGet("/status/{data}", async (string data) =>
        {
            if (!DateTime.TryParse(data, out var parsedDate))
            {
                return Results.BadRequest("Data inválida");
            }

            return Results.Ok(new
            {
                message = "status da consolidação",
                data = parsedDate.ToString("yyyy-MM-dd"),
                status = "available",
                timestamp = DateTime.UtcNow
            });
        })
        .WithName("StatusConsolidacao")
        .WithSummary("Verificar status da consolidação")
        .WithDescription("Retorna o status da consolidação para uma data específica");
    }
}