using FluxoCaixa.Consolidado.Shared.Contracts.Repositories;
using FluxoCaixa.Consolidado.Shared.Infrastructure.Database;
using FluxoCaixa.Consolidado.Shared.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FluxoCaixa.Consolidado.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddPostgreSqlDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConsolidadoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("PostgreSqlConnection")));
        
        services.AddScoped<IConsolidadoDiarioRepository, ConsolidadoDiarioRepository>();
        services.AddScoped<ILancamentoConsolidadoRepository, LancamentoConsolidadoRepository>();
        
        return services;
    }
    
    public static WebApplication EnsureDatabaseCreated(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
        context.Database.EnsureCreated();
        
        return app;
    }
}