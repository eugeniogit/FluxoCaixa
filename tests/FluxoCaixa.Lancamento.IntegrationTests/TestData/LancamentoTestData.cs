using Bogus;
using FluxoCaixa.Lancamento.Domain;
using FluxoCaixa.Lancamento.Features.CriarLancamento;

namespace FluxoCaixa.Lancamento.IntegrationTests.TestData;

public static class LancamentoTestData
{
    private static readonly Faker _faker = new("pt_BR");

    private static class TestConstants
    {
        public const decimal MinValue = 1m;
        public const decimal MaxValue = 10000m;
        public const decimal StandardMaxValue = 1000m;
        public const int RecentDaysRange = 30;
        public const int WeekDaysRange = 7;
    }

    public static CriarLancamentoRequest CreateValidLancamentoRequest()
    {
        return new CriarLancamentoRequest
        {
            Comerciante = _faker.Company.CompanyName(),
            Valor = _faker.Random.Decimal(TestConstants.MinValue, TestConstants.MaxValue),
            Tipo = _faker.Random.Enum<TipoLancamento>(),
            Data = _faker.Date.Recent(TestConstants.RecentDaysRange),
            Descricao = _faker.Commerce.ProductDescription()
        };
    }

    public static CriarLancamentoRequestBuilder CreateRequest() => new CriarLancamentoRequestBuilder();

    public static List<CriarLancamentoRequest> CreateMultipleLancamentoRequests(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => CreateValidLancamentoRequest())
            .ToList();
    }

    public static CriarLancamentoRequest CreateLancamentoRequestForComerciante(string comerciante)
    {
        return CreateRequest()
            .WithComerciante(comerciante)
            .WithRandomData()
            .Build();
    }

    public static CriarLancamentoRequest CreateLancamentoRequestForDate(DateTime date)
    {
        return CreateRequest()
            .WithData(date)
            .WithRandomData()
            .Build();
    }

    public static CriarLancamentoRequest CreateLancamentoRequestWithSpecificData(
        string comerciante,
        decimal valor,
        TipoLancamento tipo,
        DateTime data,
        string descricao = "")
    {
        return CreateRequest()
            .WithComerciante(comerciante)
            .WithValor(valor)
            .WithTipo(tipo)
            .WithData(data)
            .WithDescricao(string.IsNullOrEmpty(descricao) ? _faker.Commerce.ProductDescription() : descricao)
            .Build();
    }
}

public class CriarLancamentoRequestBuilder
{
    private static readonly Faker _faker = new("pt_BR");
    private readonly CriarLancamentoRequest _request = new();

    public CriarLancamentoRequestBuilder WithComerciante(string comerciante)
    {
        _request.Comerciante = comerciante;
        return this;
    }

    public CriarLancamentoRequestBuilder WithValor(decimal valor)
    {
        _request.Valor = valor;
        return this;
    }

    public CriarLancamentoRequestBuilder WithTipo(TipoLancamento tipo)
    {
        _request.Tipo = tipo;
        return this;
    }

    public CriarLancamentoRequestBuilder WithData(DateTime data)
    {
        _request.Data = data;
        return this;
    }

    public CriarLancamentoRequestBuilder WithDescricao(string descricao)
    {
        _request.Descricao = descricao;
        return this;
    }

    public CriarLancamentoRequestBuilder WithRandomData()
    {
        if (string.IsNullOrEmpty(_request.Comerciante))
            _request.Comerciante = _faker.Company.CompanyName();
        
        if (_request.Valor == 0)
            _request.Valor = _faker.Random.Decimal(1, 1000);
        
        if (_request.Data == default)
            _request.Data = _faker.Date.Recent(7);
        
        if (string.IsNullOrEmpty(_request.Descricao))
            _request.Descricao = _faker.Commerce.ProductDescription();

        return this;
    }

    public CriarLancamentoRequest Build() => _request;
}