using FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;
using FluxoCaixa.Consolidado.Shared.Contracts.Repositories;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace FluxoCaixa.Consolidado.Entdpoints;

public static class Consolidado
{
    public static void MapConsolidadoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/consolidado").WithTags("Consolidação");

        // Endpoint para listar todos os consolidados de um comerciante
        group.MapGet("/comerciante/{comerciante}", async (string comerciante, IConsolidadoDiarioRepository repository) =>
        {
            if (string.IsNullOrWhiteSpace(comerciante))
            {
                return Results.BadRequest("Nome do comerciante é obrigatório");
            }

            var consolidados = await repository.GetByComerciante(comerciante);
            
            if (!consolidados.Any())
            {
                return Results.NotFound($"Nenhum consolidado encontrado para o comerciante: {comerciante}");
            }

            var response = consolidados.Select(c => new
            {
                id = c.Id,
                comerciante = c.Comerciante,
                data = c.Data.ToString("yyyy-MM-dd"),
                totalCreditos = c.TotalCreditos,
                totalDebitos = c.TotalDebitos,
                saldoLiquido = c.SaldoLiquido,
                quantidadeCreditos = c.QuantidadeCreditos,
                quantidadeDebitos = c.QuantidadeDebitos,
                ultimaAtualizacao = c.UltimaAtualizacao
            }).OrderByDescending(c => c.data);

            return Results.Ok(new
            {
                comerciante,
                totalRegistros = consolidados.Count,
                consolidados = response
            });
        })
        .WithName("ListarConsolidadosPorComerciante")
        .WithSummary("Listar consolidados por comerciante")
        .WithDescription("Retorna todos os consolidados de um comerciante específico");

        // Endpoint existente para consolidação geral
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
    }
}

public class ConsolidarComercianteRequest
{
    [Required(ErrorMessage = "Data início é obrigatória")]
    public DateTime DataInicio { get; set; }

    [Required(ErrorMessage = "Data fim é obrigatória")]
    public DateTime DataFim { get; set; }
}