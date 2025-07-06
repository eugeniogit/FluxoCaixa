using Bogus;
using FluxoCaixa.Consolidado.Domain;
using FluxoCaixa.Consolidado.Features.ConsolidarPeriodo;

namespace FluxoCaixa.Consolidado.IntegrationTests.TestData;

public static class ConsolidadoTestData
{
    private static readonly Faker _faker = new("pt_BR");

    private static class TestConstants
    {
        public const decimal MinValue = 10m;
        public const decimal MaxCreditValue = 1000m;
        public const decimal MaxDebitValue = 500m;
        public const decimal DefaultTotalCreditos = 1000m;
        public const decimal DefaultTotalDebitos = 500m;
        public const int DefaultCreditCount = 2;
        public const int DefaultDebitCount = 2;
    }

    public static ConsolidarPeriodoRequest CreateConsolidarPeriodoRequest(DateTime? dataInicio = null, DateTime? dataFim = null, string? comerciante = null)
    {
        var testDate = GetTestDate();
        return new ConsolidarPeriodoRequest
        {
            DataInicio = dataInicio ?? testDate,
            DataFim = dataFim ?? testDate,
            Comerciante = comerciante
        };
    }

    public static List<LancamentoEvent> CreateMockLancamentos(string comerciante, DateTime data, int creditCount = TestConstants.DefaultCreditCount, int debitCount = TestConstants.DefaultDebitCount)
    {
        var lancamentos = new List<LancamentoEvent>();

        lancamentos.AddRange(CreateCredits(comerciante, data, creditCount));
        lancamentos.AddRange(CreateDebits(comerciante, data, debitCount));

        return lancamentos;
    }

    private static IEnumerable<LancamentoEvent> CreateCredits(string comerciante, DateTime data, int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateLancamentoEvent(comerciante, _faker.Random.Decimal(TestConstants.MinValue, TestConstants.MaxCreditValue), TipoLancamento.Credito, data));
    }

    private static IEnumerable<LancamentoEvent> CreateDebits(string comerciante, DateTime data, int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateLancamentoEvent(comerciante, _faker.Random.Decimal(TestConstants.MinValue, TestConstants.MaxDebitValue), TipoLancamento.Debito, data));
    }

    public static LancamentoEvent CreateLancamentoEvent(
        string? comerciante = null, 
        decimal? valor = null, 
        TipoLancamento? tipo = null,
        DateTime? data = null)
    {
        return new LancamentoEvent
        {
            Id = Guid.NewGuid().ToString(),
            Comerciante = comerciante ?? _faker.Company.CompanyName(),
            Valor = valor ?? _faker.Random.Decimal(TestConstants.MinValue, TestConstants.MaxCreditValue),
            Tipo = tipo ?? _faker.Random.Enum<TipoLancamento>(),
            Data = data ?? GetTestDate(),
            Descricao = _faker.Commerce.ProductDescription(),
            DataLancamento = DateTime.UtcNow
        };
    }

    public static ConsolidadoDiario CreateConsolidadoDiario(
        string comerciante, 
        DateTime data,
        decimal totalCreditos = TestConstants.DefaultTotalCreditos,
        decimal totalDebitos = TestConstants.DefaultTotalDebitos)
    {
        return new ConsolidadoDiario(comerciante, GetUtcDate(data));
    }

    public static ConsolidadoDiarioBuilder CreateConsolidadoDiarioBuilder(string comerciante, DateTime data)
    {
        return new ConsolidadoDiarioBuilder()
            .WithComerciante(comerciante)
            .WithData(data);
    }

    private static DateTime GetTestDate() => GetUtcDate(DateTime.Today);

    private static DateTime GetUtcDate(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }
}

public class ConsolidadoDiarioBuilder
{
    private string _comerciante = string.Empty;
    private DateTime _data = DateTime.Today;
    private decimal _totalCreditos = 0;
    private decimal _totalDebitos = 0;

    public ConsolidadoDiarioBuilder WithComerciante(string comerciante)
    {
        _comerciante = comerciante;
        return this;
    }

    public ConsolidadoDiarioBuilder WithData(DateTime data)
    {
        _data = data;
        return this;
    }

    public ConsolidadoDiarioBuilder WithCreditos(decimal valor)
    {
        _totalCreditos = valor;
        return this;
    }

    public ConsolidadoDiarioBuilder WithDebitos(decimal valor)
    {
        _totalDebitos = valor;
        return this;
    }

    public ConsolidadoDiario Build()
    {
        var consolidado = new ConsolidadoDiario(_comerciante, _data);
        
        if (_totalCreditos > 0)
            consolidado.AdicionarCredito(_totalCreditos);
        
        if (_totalDebitos > 0)
            consolidado.AdicionarDebito(_totalDebitos);

        return consolidado;
    }
}