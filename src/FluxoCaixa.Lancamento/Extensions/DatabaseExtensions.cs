using FluxoCaixa.Lancamento.Shared.Configurations;
using FluxoCaixa.Lancamento.Shared.Contracts.Database;
using FluxoCaixa.Lancamento.Shared.Infrastructure.Database;

namespace FluxoCaixa.Lancamento.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddMongoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDbSettings"));
        
        services.AddSingleton<IDbContext, DbContext>();
        
        return services;
    }
}