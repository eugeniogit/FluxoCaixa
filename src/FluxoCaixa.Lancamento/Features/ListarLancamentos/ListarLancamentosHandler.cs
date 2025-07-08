using FluxoCaixa.Lancamento.Shared.Contracts.Database;
using MediatR;
using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Features.ListarLancamentos;

public class ListarLancamentosHandler : IRequestHandler<ListarLancamentosQuery, ListarLancamentosResponse>
{
    private readonly IDbContext _context;

    public ListarLancamentosHandler(IDbContext context)
    {
        _context = context;
    }

    public async Task<ListarLancamentosResponse> Handle(ListarLancamentosQuery request, CancellationToken cancellationToken)
    {
        var filter = BuildFilter(request);
        var lancamentos = await GetLancamentos(filter, cancellationToken);

        return CreateResponse(lancamentos);
    }

    private FilterDefinition<Shared.Domain.Entities.Lancamento> BuildFilter(ListarLancamentosQuery request)
    {
        var filterBuilder = Builders<Shared.Domain.Entities.Lancamento>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(request.Comerciante))
            filter &= filterBuilder.Eq(l => l.Comerciante, request.Comerciante);

        filter &= filterBuilder.Gte(l => l.Data, request.DataInicio);
        filter &= filterBuilder.Lte(l => l.Data, request.DataFim);
        filter &= filterBuilder.Eq(l => l.Consolidado, request.Consolidado);

        return filter;
    }

    private async Task<List<Shared.Domain.Entities.Lancamento>> GetLancamentos(FilterDefinition<Shared.Domain.Entities.Lancamento> filter, CancellationToken cancellationToken)
    {
        return await _context.Lancamentos
            .Find(filter)
            .SortByDescending(l => l.DataLancamento)
            .ToListAsync(cancellationToken);
    }

    private static ListarLancamentosResponse CreateResponse(List<Shared.Domain.Entities.Lancamento> lancamentos)
    {
        return new ListarLancamentosResponse
        {
            Lancamentos = lancamentos.Select(LancamentoDto.FromLancamento).ToList()
        };
    }
}