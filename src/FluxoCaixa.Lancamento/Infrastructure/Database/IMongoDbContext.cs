using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Infrastructure.Database;

public interface IMongoDbContext
{
    IMongoCollection<Domain.Lancamento> Lancamentos { get; }
}