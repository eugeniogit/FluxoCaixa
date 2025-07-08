using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Shared.Contracts.Database;

public interface IDbContext
{
    IMongoCollection<Domain.Entities.Lancamento> Lancamentos { get; }
}