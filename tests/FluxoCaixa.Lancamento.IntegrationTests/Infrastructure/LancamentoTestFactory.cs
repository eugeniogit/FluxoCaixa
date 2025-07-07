using FluxoCaixa.Lancamento.Configuration;
using FluxoCaixa.Lancamento.Features.CriarLancamento;
using FluxoCaixa.Lancamento.Infrastructure.Database;
using FluxoCaixa.Lancamento.Infrastructure.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;
using Xunit;

namespace FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;

public class LancamentoTestFactory : IAsyncLifetime
{
    private readonly MongoDbContainer _mongoContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private ServiceProvider? _serviceProvider;

    public LancamentoTestFactory()
    {
        _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .WithUsername("admin")
            .WithPassword("password")
            .WithPortBinding(27017, true)
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.12-management")
            .WithUsername("admin")
            .WithPassword("password")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        await _mongoContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole());
        
        services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = _mongoContainer.GetConnectionString();
            options.DatabaseName = "FluxoCaixaTest";
        });

        services.AddSingleton<IMongoDbContext, MongoDbContext>();

        services.Configure<RabbitMqSettings>(options =>
        {
            options.HostName = _rabbitMqContainer.Hostname;
            options.Port = _rabbitMqContainer.GetMappedPublicPort(5672);
            options.UserName = "admin";
            options.Password = "password";
            options.QueueName = "lancamento_events";
        });

        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CriarLancamentoHandler).Assembly));
    }

    public T GetService<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Service provider not initialized");
        
        return _serviceProvider.GetRequiredService<T>();
    }

    public IMongoDbContext GetDbContext()
    {
        return GetService<IMongoDbContext>();
    }

    public IMediator GetMediator()
    {
        return GetService<IMediator>();
    }

    public string GetMongoConnectionString()
    {
        return _mongoContainer.GetConnectionString();
    }

    public string GetRabbitMqConnectionString()
    {
        return _rabbitMqContainer.GetConnectionString();
    }

    public async Task ClearDatabaseAsync()
    {
        var dbContext = GetDbContext();
        await dbContext.Lancamentos.DeleteManyAsync(_ => true);
    }
}