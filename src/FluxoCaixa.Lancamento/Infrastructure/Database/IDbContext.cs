using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Infrastructure.Database;

public interface IDbContext
{
    IMongoCollection<Domain.Lancamento> Lancamentos { get; }
}