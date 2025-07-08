using FluxoCaixa.Lancamento.Shared.Configurations;
using FluxoCaixa.Lancamento.Shared.Contracts.Database;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Shared.Infrastructure.Database;

public class DbContext : IDbContext
{
    private readonly IMongoDatabase _database;

    public DbContext(IOptions<MongoDbSettings> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<Domain.Entities.Lancamento> Lancamentos =>
        _database.GetCollection<Domain.Entities.Lancamento>("lancamentos");
}