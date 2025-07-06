using FluxoCaixa.Lancamento.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Infrastructure.Database;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<Domain.Lancamento> Lancamentos =>
        _database.GetCollection<Domain.Lancamento>("lancamentos");
}