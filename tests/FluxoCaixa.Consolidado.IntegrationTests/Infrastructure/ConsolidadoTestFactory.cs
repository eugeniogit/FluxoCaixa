using FluxoCaixa.Consolidado.Infrastructure.Database;
using FluxoCaixa.Consolidado.Infrastructure.ExternalServices;
using FluxoCaixa.Consolidado.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace FluxoCaixa.Consolidado.IntegrationTests.Infrastructure;

public class ConsolidadoTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private RabbitMqContainer _rabbitMqContainer = null!;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("fluxocaixa_test")
            .WithUsername("admin")
            .WithPassword("password")
            .WithPortBinding(5432, true)
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("admin")
            .WithPassword("password")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .Build();

        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSqlConnection"] = _postgresContainer.GetConnectionString(),
                ["RabbitMqSettings:HostName"] = _rabbitMqContainer.Hostname,
                ["RabbitMqSettings:Port"] = _rabbitMqContainer.GetMappedPublicPort(5672).ToString(),
                ["RabbitMqSettings:UserName"] = "admin",
                ["RabbitMqSettings:Password"] = "password",
                ["RabbitMqSettings:QueueName"] = "lancamento_events_test",
                ["LancamentoApiSettings:BaseUrl"] = "http://localhost:5000",
                ["LancamentoApiSettings:ApiKey"] = "test-api-key-consolidado"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove hosted services for testing to avoid background job execution
            var hostedServices = services.Where(s => s.ServiceType == typeof(IHostedService)).ToList();
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }

            // Replace LancamentoApiClient with mock
            var lancamentoApiDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ILancamentoApiClient));
            if (lancamentoApiDescriptor != null)
            {
                services.Remove(lancamentoApiDescriptor);
            }
            services.AddSingleton<ILancamentoApiClient, MockLancamentoApiClient>();

            // Replace RabbitMqPublisher with mock  
            var rabbitMqPublisherDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(FluxoCaixa.Consolidado.Infrastructure.Messaging.IRabbitMqPublisher));
            if (rabbitMqPublisherDescriptor != null)
            {
                services.Remove(rabbitMqPublisherDescriptor);
            }
            services.AddSingleton<FluxoCaixa.Consolidado.Infrastructure.Messaging.IRabbitMqPublisher, MockRabbitMqPublisher>();

            // Ensure database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
            context.Database.EnsureCreated();
        });
    }

    public string GetPostgreSqlConnectionString() => _postgresContainer.GetConnectionString();

    public string GetRabbitMqConnectionString() => 
        $"amqp://admin:password@{_rabbitMqContainer.Hostname}:{_rabbitMqContainer.GetMappedPublicPort(5672)}";

    public override async ValueTask DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _rabbitMqContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }
}

public class MockLancamentoApiClient : ILancamentoApiClient
{
    private const decimal MockCreditValue = 100.00m;
    private const decimal MockDebitValue = 50.00m;
    private const string DefaultMockMerchant = "Loja Teste Mock";
    private const string SpecificTestMerchant = "Loja Teste Específica";

    public Task<List<LancamentoEvent>> GetLancamentosByPeriodoAsync(
        DateTime dataInicio, 
        DateTime dataFim, 
        string? comerciante = null,
        bool? consolidado = null)
    {
        var mockLancamentos = new List<LancamentoEvent>();
        
        if (ShouldReturnMockData(comerciante))
        {
            // Para o período, criar lançamentos para cada dia
            for (var data = dataInicio.Date; data <= dataFim.Date; data = data.AddDays(1))
            {
                mockLancamentos.AddRange(CreateMockLancamentos(comerciante, data));
            }
        }

        // Filtrar por consolidado se especificado
        if (consolidado.HasValue)
        {
            // Para testes, retornar lançamentos baseado no filtro consolidado
            // Se consolidado = false, retornar todos (simulando que nenhum foi consolidado ainda)
            // Se consolidado = true, retornar lista vazia (simulando que nenhum foi consolidado ainda)
            if (consolidado.Value)
            {
                mockLancamentos.Clear();
            }
        }
        
        return Task.FromResult(mockLancamentos);
    }

    private static bool ShouldReturnMockData(string? comerciante)
    {
        return string.IsNullOrEmpty(comerciante) || comerciante == SpecificTestMerchant;
    }

    private static IEnumerable<LancamentoEvent> CreateMockLancamentos(string? comerciante, DateTime data)
    {
        var merchantName = comerciante ?? DefaultMockMerchant;
        var testDate = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);

        return new[]
        {
            CreateMockLancamento(merchantName, MockCreditValue, TipoLancamento.Credito, testDate, "Mock Credit Transaction"),
            CreateMockLancamento(merchantName, MockDebitValue, TipoLancamento.Debito, testDate, "Mock Debit Transaction")
        };
    }

    private static LancamentoEvent CreateMockLancamento(string comerciante, decimal valor, TipoLancamento tipo, DateTime data, string descricao)
    {
        return new LancamentoEvent
        {
            Id = Guid.NewGuid().ToString(),
            Comerciante = comerciante,
            Valor = valor,
            Tipo = tipo,
            Data = data,
            Descricao = descricao,
            DataLancamento = DateTime.UtcNow
        };
    }

    public Task MarcarLancamentosComoConsolidadosAsync(List<string> lancamentoIds)
    {
        // Para testes, apenas simular que marcou como consolidados
        return Task.CompletedTask;
    }
}

public class MockRabbitMqPublisher : FluxoCaixa.Consolidado.Infrastructure.Messaging.IRabbitMqPublisher
{
    public Task PublishMarcarConsolidadosEventAsync(FluxoCaixa.Consolidado.Infrastructure.Messaging.MarcarConsolidadosEvent marcarConsolidadosEvent)
    {
        // Para testes, apenas simular que enviou o evento
        return Task.CompletedTask;
    }
}