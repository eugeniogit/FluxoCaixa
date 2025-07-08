using FluentAssertions;
using FluxoCaixa.Lancamento.Shared.Domain.Entities;
using MongoDB.Bson;
using Xunit;

namespace FluxoCaixa.Lancamento.UnitTests.Domain;

public class LancamentoTests
{
    [Fact]
    public void Lancamento_DeveSerCriadoComSucesso_QuandoParametrosValidos()
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var valor = 100.50m;
        var tipo = TipoLancamento.Credito;
        var data = DateTime.Now;
        var descricao = "Descrição teste";

        // Act
        var lancamento = new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);

        // Assert
        lancamento.Should().NotBeNull();
        lancamento.Id.Should().NotBeNullOrEmpty();
        lancamento.Comerciante.Should().Be(comerciante);
        lancamento.Valor.Should().Be(valor);
        lancamento.Tipo.Should().Be(tipo);
        lancamento.Data.Should().Be(data);
        lancamento.Descricao.Should().Be(descricao);
        lancamento.DataLancamento.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        lancamento.Consolidado.Should().BeFalse();
    }

    [Fact]
    public void Lancamento_DeveGerarIdUnico_QuandoCriado()
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var valor = 100.50m;
        var tipo = TipoLancamento.Credito;
        var data = DateTime.Now;
        var descricao = "Descrição teste";

        // Act
        var lancamento1 = new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);
        var lancamento2 = new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);

        // Assert
        lancamento1.Id.Should().NotBe(lancamento2.Id);
        ObjectId.TryParse(lancamento1.Id, out _).Should().BeTrue();
        ObjectId.TryParse(lancamento2.Id, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Lancamento_DeveLancarExcecao_QuandoComercianteInvalido(string comerciante)
    {
        // Arrange
        var valor = 100.50m;
        var tipo = TipoLancamento.Credito;
        var data = DateTime.Now;
        var descricao = "Descrição teste";

        // Act & Assert
        var act = () => new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Comerciante é obrigatório*")
            .And.ParamName.Should().Be("comerciante");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Lancamento_DeveLancarExcecao_QuandoValorInvalido(decimal valor)
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var tipo = TipoLancamento.Credito;
        var data = DateTime.Now;
        var descricao = "Descrição teste";

        // Act & Assert
        var act = () => new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Valor deve ser maior que zero*")
            .And.ParamName.Should().Be("valor");
    }

    [Fact]
    public void Lancamento_DeveLancarExcecao_QuandoDataInvalida()
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var valor = 100.50m;
        var tipo = TipoLancamento.Credito;
        var data = default(DateTime);
        var descricao = "Descrição teste";

        // Act & Assert
        var act = () => new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Data é obrigatória*")
            .And.ParamName.Should().Be("data");
    }

    [Fact]
    public void IsCredito_DeveRetornarTrue_QuandoTipoCredito()
    {
        // Arrange
        var lancamento = new Shared.Domain.Entities.Lancamento("Comerciante", 100m, TipoLancamento.Credito, DateTime.Now, "Descrição");

        // Act
        var isCredito = lancamento.IsCredito();

        // Assert
        isCredito.Should().BeTrue();
    }

    [Fact]
    public void IsCredito_DeveRetornarFalse_QuandoTipoDebito()
    {
        // Arrange
        var lancamento = new Shared.Domain.Entities.Lancamento("Comerciante", 100m, TipoLancamento.Debito, DateTime.Now, "Descrição");

        // Act
        var isCredito = lancamento.IsCredito();

        // Assert
        isCredito.Should().BeFalse();
    }

    [Fact]
    public void IsDebito_DeveRetornarTrue_QuandoTipoDebito()
    {
        // Arrange
        var lancamento = new Shared.Domain.Entities.Lancamento("Comerciante", 100m, TipoLancamento.Debito, DateTime.Now, "Descrição");

        // Act
        var isDebito = lancamento.IsDebito();

        // Assert
        isDebito.Should().BeTrue();
    }

    [Fact]
    public void IsDebito_DeveRetornarFalse_QuandoTipoCredito()
    {
        // Arrange
        var lancamento = new Shared.Domain.Entities.Lancamento("Comerciante", 100m, TipoLancamento.Credito, DateTime.Now, "Descrição");

        // Act
        var isDebito = lancamento.IsDebito();

        // Assert
        isDebito.Should().BeFalse();
    }

    [Fact]
    public void MarcarComoConsolidado_DeveAlterarConsolidadoParaTrue()
    {
        // Arrange
        var lancamento = new Shared.Domain.Entities.Lancamento("Comerciante", 100m, TipoLancamento.Credito, DateTime.Now, "Descrição");
        lancamento.Consolidado.Should().BeFalse();

        // Act
        lancamento.MarcarComoConsolidado();

        // Assert
        lancamento.Consolidado.Should().BeTrue();
    }

    [Theory]
    [InlineData(TipoLancamento.Credito)]
    [InlineData(TipoLancamento.Debito)]
    public void Lancamento_DeveManterTipoLancamento_QuandoCriado(TipoLancamento tipo)
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var valor = 100.50m;
        var data = DateTime.Now;
        var descricao = "Descrição teste";

        // Act
        var lancamento = new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);

        // Assert
        lancamento.Tipo.Should().Be(tipo);
    }

    [Fact]
    public void Lancamento_DeveAceitarDescricaoVazia_QuandoCriado()
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var valor = 100.50m;
        var tipo = TipoLancamento.Credito;
        var data = DateTime.Now;
        var descricao = "";

        // Act
        var lancamento = new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);

        // Assert
        lancamento.Descricao.Should().BeEmpty();
    }

    [Fact]
    public void Lancamento_DeveDefinirDataLancamentoComoUtcNow_QuandoCriado()
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var valor = 100.50m;
        var tipo = TipoLancamento.Credito;
        var data = DateTime.Now;
        var descricao = "Descrição teste";
        var antes = DateTime.UtcNow;

        // Act
        var lancamento = new Shared.Domain.Entities.Lancamento(comerciante, valor, tipo, data, descricao);
        var depois = DateTime.UtcNow;

        // Assert
        lancamento.DataLancamento.Should().BeOnOrAfter(antes);
        lancamento.DataLancamento.Should().BeOnOrBefore(depois);
    }
}