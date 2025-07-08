using FluxoCaixa.Consolidado.Features.ConsolidarLancamento;
using FluxoCaixa.Consolidado.Shared.Contracts.ExternalServices;
using FluxoCaixa.Consolidado.Shared.Contracts.Messaging;
using FluxoCaixa.Consolidado.Shared.Contracts.Repositories;
using FluxoCaixa.Consolidado.Shared.Infrastructure.Database;
using FluxoCaixa.Consolidado.Shared.Infrastructure.Messaging;
using FluxoCaixa.Consolidado.Shared.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace FluxoCaixa.Consolidado.IntegrationTests.Infrastructure;

public class ConsolidadoTestFactory : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private ServiceProvider? _serviceProvider;

    public ConsolidadoTestFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("fluxocaixa")
            .WithUsername("admin")
            .WithPassword("password")
            .WithPortBinding(5432, true)
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
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Initialize database
        await InitializeDatabase();
    }

    public async Task DisposeAsync()
    {
        try
        {
            // Dispose services first to close RabbitMQ connections properly
            if (_serviceProvider != null)
            {
                // Get and dispose messaging services explicitly
                try
                {
                    var publisher = _serviceProvider.GetService<LancamentosConsolidadosPublisher>();
                    publisher?.Dispose();
                    
                    var consumer = _serviceProvider.GetService<LancamentoConsumer>();
                    consumer?.Dispose();
                }
                catch { }
                
                _serviceProvider.Dispose();
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw during dispose
            Console.WriteLine($"Error disposing services: {ex.Message}");
        }
        
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole());

        // Database
        services.AddDbContext<ConsolidadoDbContext>(options =>
            options.UseNpgsql(_postgresContainer.GetConnectionString()));

        // Repositories
        services.AddScoped<IConsolidadoDiarioRepository, ConsolidadoDiarioRepository>();
        services.AddScoped<ILancamentoConsolidadoRepository, LancamentoConsolidadoRepository>();

        // Mock External Services
        services.AddScoped<ILancamentoApiClient>(provider =>
        {
            var mock = new Mock<ILancamentoApiClient>();
            return mock.Object;
        });

        // RabbitMQ
        services.Configure<MessageBrokerSettings>(options =>
        {
            options.HostName = _rabbitMqContainer.Hostname;
            options.Port = _rabbitMqContainer.GetMappedPublicPort(5672);
            options.UserName = "admin";
            options.Password = "password";
            options.QueueName = "lancamento_events";
        });

        services.AddSingleton<IMessageBrokerFactory, MessageBrokerFactory>();
        services.AddSingleton<LancamentosConsolidadosPublisher>();
        services.AddSingleton<IMessagePublisher>(provider => 
            provider.GetRequiredService<IMessageBrokerFactory>().CreatePublisher());

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ConsolidarLancamentoHandler).Assembly));
    }

    private async Task InitializeDatabase()
    {
        using var scope = _serviceProvider!.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public T GetService<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("Service provider not initialized");
        
        return _serviceProvider.GetRequiredService<T>();
    }

    public ConsolidadoDbContext GetDbContext()
    {
        return GetService<ConsolidadoDbContext>();
    }

    public IMediator GetMediator()
    {
        return GetService<IMediator>();
    }

    public Mock<ILancamentoApiClient> GetMockLancamentoApiClient()
    {
        var service = GetService<ILancamentoApiClient>();
        return Mock.Get(service);
    }

    public string GetPostgresConnectionString()
    {
        return _postgresContainer.GetConnectionString();
    }

    public string GetRabbitMqConnectionString()
    {
        return _rabbitMqContainer.GetConnectionString();
    }

    public void CleanupMessaging()
    {
        try
        {
            // Force disposal of messaging components to avoid connection issues
            var publisher = _serviceProvider?.GetService<LancamentosConsolidadosPublisher>();
            if (publisher is IDisposable disposablePublisher)
            {
                disposablePublisher.Dispose();
            }

            var consumer = _serviceProvider?.GetService<LancamentoConsumer>();
            if (consumer is IDisposable disposableConsumer)
            {
                disposableConsumer.Dispose();
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}