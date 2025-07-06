using FluxoCaixa.Lancamento.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;
using Xunit;
using DotNet.Testcontainers.Builders;

namespace FluxoCaixa.Lancamento.IntegrationTests.Infrastructure;

public class LancamentoTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string MongoUsername = "admin";
    private const string MongoPassword = "password";
    private const string RabbitMqUsername = "admin";
    private const string RabbitMqPassword = "password";
    private const int MongoPort = 27017;
    private const int RabbitMqPort = 5672;
    private const int RabbitMqManagementPort = 15672;
    private const string TestApiKey = "test-api-key-integration-tests";
    private const string TestDatabaseName = "fluxocaixa_lancamentos_test";
    private const string TestQueueName = "lancamento_events_test";

    private MongoDbContainer _mongoContainer = null!;
    private RabbitMqContainer _rabbitMqContainer = null!;

    public async Task InitializeAsync()
    {
        _mongoContainer = CreateMongoContainer();
        _rabbitMqContainer = CreateRabbitMqContainer();

        await Task.WhenAll(
            _mongoContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        );
    }

    private MongoDbContainer CreateMongoContainer()
    {
        return new MongoDbBuilder()
            .WithImage("mongo:latest")
            .WithUsername(MongoUsername)
            .WithPassword(MongoPassword)
            .WithPortBinding(MongoPort, true)
            .Build();
    }

    private RabbitMqContainer CreateRabbitMqContainer()
    {
        return new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername(RabbitMqUsername)
            .WithPassword(RabbitMqPassword)
            .WithPortBinding(RabbitMqPort, true)
            .WithPortBinding(RabbitMqManagementPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RabbitMqPort))
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(ConfigureTestConfiguration);
        builder.ConfigureServices(ConfigureTestServices);
    }

    private void ConfigureTestConfiguration(WebHostBuilderContext context, IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(CreateTestConfiguration());
    }

    private Dictionary<string, string?> CreateTestConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["MongoDbSettings:ConnectionString"] = _mongoContainer.GetConnectionString(),
            ["MongoDbSettings:DatabaseName"] = TestDatabaseName,
            ["RabbitMqSettings:HostName"] = _rabbitMqContainer.Hostname,
            ["RabbitMqSettings:Port"] = _rabbitMqContainer.GetMappedPublicPort(RabbitMqPort).ToString(),
            ["RabbitMqSettings:UserName"] = RabbitMqUsername,
            ["RabbitMqSettings:Password"] = RabbitMqPassword,
            ["RabbitMqSettings:QueueName"] = TestQueueName,
            ["ApiKeySettings:ValidApiKeys:0:Id"] = "test",
            ["ApiKeySettings:ValidApiKeys:0:Name"] = "Test Client",
            ["ApiKeySettings:ValidApiKeys:0:Key"] = TestApiKey,
            ["ApiKeySettings:ValidApiKeys:0:IsActive"] = "true"
        };
    }

    private static void ConfigureTestServices(IServiceCollection services)
    {
        // Override any services if needed for testing
    }

    public string GetValidApiKey() => TestApiKey;

    public string GetMongoConnectionString() => _mongoContainer.GetConnectionString();

    public string GetRabbitMqConnectionString() => 
        $"amqp://{RabbitMqUsername}:{RabbitMqPassword}@{_rabbitMqContainer.Hostname}:{_rabbitMqContainer.GetMappedPublicPort(RabbitMqPort)}";

    public override async ValueTask DisposeAsync()
    {
        await DisposeContainersAsync();
        await base.DisposeAsync();
    }

    private async Task DisposeContainersAsync()
    {
        await Task.WhenAll(
            _mongoContainer.StopAsync(),
            _rabbitMqContainer.StopAsync()
        );

        await Task.WhenAll(
            _mongoContainer.DisposeAsync().AsTask(),
            _rabbitMqContainer.DisposeAsync().AsTask()
        );
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }
}