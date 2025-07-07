namespace FluxoCaixa.Consolidado.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "FluxoCaixa Consolidado API", Version = "v1" });
        });
        
        return services;
    }
}