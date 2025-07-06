using FluxoCaixa.Lancamento.Infrastructure.Database;
using FluxoCaixa.Lancamento.Infrastructure.Messaging;
using FluxoCaixa.Lancamento.Configuration;
using FluxoCaixa.Lancamento.Extensions;
using FluentValidation;
using MediatR;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FluxoCaixa Lancamento API", Version = "v1" });
    
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API Key para autenticação"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMqSettings"));

builder.Services.AddApiKeyAuthentication(builder.Configuration);

builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<MarcarConsolidadosConsumer>();
builder.Services.AddHostedService<MarcarConsolidadosBackgroundService>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapLancamentoEndpoints();
app.MapHealthCheckEndpoints();

app.Run();

public partial class Program { }