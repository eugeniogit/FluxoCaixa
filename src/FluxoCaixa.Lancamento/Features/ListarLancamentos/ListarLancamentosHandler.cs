using FluxoCaixa.Lancamento.Infrastructure.Database;
using MediatR;
using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Features.ListarLancamentos;

public class ListarLancamentosHandler : IRequestHandler<ListarLancamentosQuery, ListarLancamentosResponse>
{
    private readonly IMongoDbContext _context;

    public ListarLancamentosHandler(IMongoDbContext context)
    {
        _context = context;
    }

    public async Task<ListarLancamentosResponse> Handle(ListarLancamentosQuery request, CancellationToken cancellationToken)
    {
        var filter = BuildFilter(request);
        var lancamentos = await GetLancamentos(filter, cancellationToken);

        return CreateResponse(lancamentos);
    }

    private FilterDefinition<Domain.Lancamento> BuildFilter(ListarLancamentosQuery request)
    {
        var filterBuilder = Builders<Domain.Lancamento>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(request.Comerciante))
            filter &= filterBuilder.Eq(l => l.Comerciante, request.Comerciante);

        filter &= filterBuilder.Gte(l => l.Data, request.DataInicio);
        filter &= filterBuilder.Lte(l => l.Data, request.DataFim);
        filter &= filterBuilder.Eq(l => l.Consolidado, request.Consolidado);

        return filter;
    }

    private async Task<List<Domain.Lancamento>> GetLancamentos(FilterDefinition<Domain.Lancamento> filter, CancellationToken cancellationToken)
    {
        return await _context.Lancamentos
            .Find(filter)
            .SortByDescending(l => l.DataLancamento)
            .ToListAsync(cancellationToken);
    }

    private static ListarLancamentosResponse CreateResponse(List<Domain.Lancamento> lancamentos)
    {
        return new ListarLancamentosResponse
        {
            Lancamentos = lancamentos.Select(LancamentoDto.FromLancamento).ToList()
        };
    }
}