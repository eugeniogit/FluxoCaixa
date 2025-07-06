using FluxoCaixa.Consolidado.Configuration;
using FluxoCaixa.Consolidado.Extensions;
using FluxoCaixa.Consolidado.Infrastructure.Database;
using FluxoCaixa.Consolidado.Infrastructure.ExternalServices;
using FluxoCaixa.Consolidado.Infrastructure.Messaging;
using FluxoCaixa.Consolidado.Infrastructure.Repositories;
using FluxoCaixa.Consolidado.Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FluxoCaixa Consolidado API", Version = "v1" });
});

builder.Services.AddDbContext<ConsolidadoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection")));

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSqlConnection")!);

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMqSettings"));
builder.Services.Configure<LancamentoApiSettings>(
    builder.Configuration.GetSection("LancamentoApiSettings"));
builder.Services.Configure<ConsolidationSettings>(
    builder.Configuration.GetSection("ConsolidationSettings"));

builder.Services.AddSingleton<IRabbitMqConsumer, RabbitMqConsumer>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<RabbitMqBackgroundService>();

builder.Services.AddHttpClient<ILancamentoApiClient, LancamentoApiClient>();

// Add Repositories
builder.Services.AddScoped<IConsolidadoDiarioRepository, ConsolidadoDiarioRepository>();
builder.Services.AddScoped<ILancamentoProcessadoRepository, LancamentoProcessadoRepository>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("ConsolidacaoDiariaJob");
    q.AddJob<ConsolidacaoDiariaJob>(opts => opts.WithIdentity(jobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("ConsolidacaoDiariaJob-trigger")
        .WithCronSchedule(Constants.Scheduling.DailyConsolidationCron)); // Executa todo dia às 01:00
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapConsolidadoEndpoints();
app.MapHealthCheckEndpoints();
app.MapTestEndpoints();

app.Run();

public partial class Program { }