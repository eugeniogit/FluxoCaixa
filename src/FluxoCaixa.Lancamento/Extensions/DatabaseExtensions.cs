using FluxoCaixa.Lancamento.Configuration;
using FluxoCaixa.Lancamento.Infrastructure.Database;

namespace FluxoCaixa.Lancamento.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddMongoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDbSettings"));
        
        services.AddSingleton<IMongoDbContext, MongoDbContext>();
        
        return services;
    }
}