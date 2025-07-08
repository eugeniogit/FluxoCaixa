using FluxoCaixa.Lancamento.Features.CriarLancamento;
using FluxoCaixa.Lancamento.Features.ListarLancamentos;
using FluentValidation;
using MediatR;

namespace FluxoCaixa.Lancamento.Endpoints;

public static class Lancamentos
{
    public static void MapLancamentoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lancamentos").WithTags("Lancamentos");

        group.MapPost("/", async (CriarLancamentoRequest request, IMediator mediator, IValidator<CriarLancamentoCommand> validator) =>
        {
            var command = new CriarLancamentoCommand
            {
                Comerciante = request.Comerciante,
                Valor = request.Valor,
                Tipo = request.Tipo,
                Data = request.Data,
                Descricao = request.Descricao
            };

            var validationResult = await validator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var response = await mediator.Send(command);
            return Results.Created($"/api/lancamentos/{response.Id}", response);
        })
        .WithName("CriarLancamento")
        .WithSummary("Criar novo lançamento")
        .WithDescription("Cria um novo lançamento de débito ou crédito")
        .RequireAuthorization();

        group.MapGet("/", async (
            IMediator mediator,
            DateTime dataInicio,
            DateTime dataFim,
            bool consolidado,
            string? comerciante = null) =>
        {
            var query = new ListarLancamentosQuery
            {
                Comerciante = comerciante,
                DataInicio = dataInicio,
                DataFim = dataFim,
                Consolidado = consolidado
            };

            var response = await mediator.Send(query);
            return Results.Ok(response);
        })
        .WithName("ListarLancamentos")
        .WithSummary("Listar lançamentos")
        .WithDescription("Lista lançamentos com filtros")
        .RequireAuthorization();
    }
}