using FluxoCaixa.Consolidado.Entdpoints;
using FluxoCaixa.Consolidado.Extensions;
using MediatR;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerConfiguration();
builder.Services.AddPostgreSqlDatabase(builder.Configuration);
builder.Services.AddHealthCheckConfiguration(builder.Configuration);
builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddQuartzScheduling(builder.Configuration);
builder.Services.AddOpenTelemetryConfiguration(builder.Configuration);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

var app = builder.Build();

app.EnsureDatabaseCreated();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapConsolidadoEndpoints();
app.MapHealthCheckEndpoints();
app.MapTestEndpoints();
app.MapPrometheusScrapingEndpoint();

app.Run();

public partial class Program { }