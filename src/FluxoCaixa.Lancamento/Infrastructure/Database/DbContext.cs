using FluxoCaixa.Lancamento.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FluxoCaixa.Lancamento.Infrastructure.Database;

public class DbContext : IDbContext
{
    private readonly IMongoDatabase _database;

    public DbContext(IOptions<MongoDbSettings> options)
    {
        var client = new MongoClient(options.Value.ConnectionString);
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<Domain.Lancamento> Lancamentos =>
        _database.GetCollection<Domain.Lancamento>("lancamentos");
}