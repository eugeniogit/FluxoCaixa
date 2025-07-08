using FluentAssertions;
using FluxoCaixa.Consolidado.Shared.Domain.Events;
using Xunit;

namespace FluxoCaixa.Consolidado.UnitTests.Domain;

public class ConsolidadoTests
{
    [Fact]
    public void Consolidado_DeveSerCriadoComSucesso_QuandoParametrosValidos()
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var data = new DateTime(2023, 12, 25);

        // Act
        var consolidado = new Shared.Domain.Entities.Consolidado(comerciante, data);

        // Assert
        consolidado.Should().NotBeNull();
        consolidado.Comerciante.Should().Be(comerciante);
        consolidado.Data.Should().Be(data.Date);
        consolidado.TotalCreditos.Should().Be(0);
        consolidado.TotalDebitos.Should().Be(0);
        consolidado.SaldoLiquido.Should().Be(0);
        consolidado.QuantidadeCreditos.Should().Be(0);
        consolidado.QuantidadeDebitos.Should().Be(0);
        consolidado.UltimaAtualizacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Consolidado_DeveLancarExcecao_QuandoComercianteInvalido(string comerciante)
    {
        // Arrange
        var data = new DateTime(2023, 12, 25);

        // Act & Assert
        var act = () => new Shared.Domain.Entities.Consolidado(comerciante, data);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Comerciante é obrigatório*")
            .And.ParamName.Should().Be("comerciante");
    }

    [Fact]
    public void Consolidado_DeveLancarExcecao_QuandoDataInvalida()
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var data = default(DateTime);

        // Act & Assert
        var act = () => new Shared.Domain.Entities.Consolidado(comerciante, data);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Data é obrigatória*")
            .And.ParamName.Should().Be("data");
    }

    [Fact]
    public void Consolidado_DeveDefinirDataSemHorario_QuandoCriado()
    {
        // Arrange
        var comerciante = "Comerciante Teste";
        var data = new DateTime(2023, 12, 25, 14, 30, 45);

        // Act
        var consolidado = new Shared.Domain.Entities.Consolidado(comerciante, data);

        // Assert
        consolidado.Data.Should().Be(data.Date);
        consolidado.Data.Hour.Should().Be(0);
        consolidado.Data.Minute.Should().Be(0);
        consolidado.Data.Second.Should().Be(0);
    }

    [Fact]
    public void AdicionarCredito_DeveIncrementarTotalCreditos_QuandoValorPositivo()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);
        var valor = 100.50m;

        // Act
        consolidado.AdicionarCredito(valor);

        // Assert
        consolidado.TotalCreditos.Should().Be(valor);
        consolidado.QuantidadeCreditos.Should().Be(1);
        consolidado.SaldoLiquido.Should().Be(valor);
        consolidado.UltimaAtualizacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void AdicionarCredito_DeveLancarExcecao_QuandoValorInvalido(decimal valor)
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);

        // Act & Assert
        var act = () => consolidado.AdicionarCredito(valor);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Valor deve ser positivo*")
            .And.ParamName.Should().Be("valor");
    }

    [Fact]
    public void AdicionarDebito_DeveIncrementarTotalDebitos_QuandoValorPositivo()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);
        var valor = 50.25m;

        // Act
        consolidado.AdicionarDebito(valor);

        // Assert
        consolidado.TotalDebitos.Should().Be(valor);
        consolidado.QuantidadeDebitos.Should().Be(1);
        consolidado.SaldoLiquido.Should().Be(-valor);
        consolidado.UltimaAtualizacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50.25)]
    public void AdicionarDebito_DeveLancarExcecao_QuandoValorInvalido(decimal valor)
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);

        // Act & Assert
        var act = () => consolidado.AdicionarDebito(valor);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Valor deve ser positivo*")
            .And.ParamName.Should().Be("valor");
    }

    [Fact]
    public void SaldoLiquido_DeveSerCalculadoCorretamente_QuandoCreditsEDebitos()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);

        // Act
        consolidado.AdicionarCredito(200m);
        consolidado.AdicionarCredito(100m);
        consolidado.AdicionarDebito(50m);
        consolidado.AdicionarDebito(25m);

        // Assert
        consolidado.TotalCreditos.Should().Be(300m);
        consolidado.TotalDebitos.Should().Be(75m);
        consolidado.SaldoLiquido.Should().Be(225m);
        consolidado.QuantidadeCreditos.Should().Be(2);
        consolidado.QuantidadeDebitos.Should().Be(2);
    }

    [Fact]
    public void Consolidar_DeveAdicionarCredito_QuandoLancamentoCredito()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);
        var lancamento = new LancamentoEvent
        {
            Id = "1",
            Comerciante = "Comerciante",
            Valor = 100m,
            Tipo = TipoLancamento.Credito,
            Data = DateTime.Now,
            Descricao = "Teste"
        };

        // Act
        consolidado.Consolidar(lancamento);

        // Assert
        consolidado.TotalCreditos.Should().Be(100m);
        consolidado.TotalDebitos.Should().Be(0m);
        consolidado.QuantidadeCreditos.Should().Be(1);
        consolidado.QuantidadeDebitos.Should().Be(0);
        consolidado.SaldoLiquido.Should().Be(100m);
    }

    [Fact]
    public void Consolidar_DeveAdicionarDebito_QuandoLancamentoDebito()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);
        var lancamento = new LancamentoEvent
        {
            Id = "1",
            Comerciante = "Comerciante",
            Valor = 75m,
            Tipo = TipoLancamento.Debito,
            Data = DateTime.Now,
            Descricao = "Teste"
        };

        // Act
        consolidado.Consolidar(lancamento);

        // Assert
        consolidado.TotalCreditos.Should().Be(0m);
        consolidado.TotalDebitos.Should().Be(75m);
        consolidado.QuantidadeCreditos.Should().Be(0);
        consolidado.QuantidadeDebitos.Should().Be(1);
        consolidado.SaldoLiquido.Should().Be(-75m);
    }

    [Fact]
    public void ConsolidarLancamentos_DeveProcessarTodosLancamentos()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);
        var lancamentos = new List<LancamentoEvent>
        {
            new LancamentoEvent
            {
                Id = "1",
                Comerciante = "Comerciante",
                Valor = 100m,
                Tipo = TipoLancamento.Credito,
                Data = DateTime.Now,
                Descricao = "Credito 1"
            },
            new LancamentoEvent
            {
                Id = "2",
                Comerciante = "Comerciante",
                Valor = 50m,
                Tipo = TipoLancamento.Debito,
                Data = DateTime.Now,
                Descricao = "Debito 1"
            },
            new LancamentoEvent
            {
                Id = "3",
                Comerciante = "Comerciante",
                Valor = 200m,
                Tipo = TipoLancamento.Credito,
                Data = DateTime.Now,
                Descricao = "Credito 2"
            }
        };

        // Act
        consolidado.ConsolidarLancamentos(lancamentos);

        // Assert
        consolidado.TotalCreditos.Should().Be(300m);
        consolidado.TotalDebitos.Should().Be(50m);
        consolidado.QuantidadeCreditos.Should().Be(2);
        consolidado.QuantidadeDebitos.Should().Be(1);
        consolidado.SaldoLiquido.Should().Be(250m);
    }

    [Fact]
    public void ConsolidarLancamentos_DeveProcessarListaVazia()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);
        var lancamentos = new List<LancamentoEvent>();

        // Act
        consolidado.ConsolidarLancamentos(lancamentos);

        // Assert
        consolidado.TotalCreditos.Should().Be(0m);
        consolidado.TotalDebitos.Should().Be(0m);
        consolidado.QuantidadeCreditos.Should().Be(0);
        consolidado.QuantidadeDebitos.Should().Be(0);
        consolidado.SaldoLiquido.Should().Be(0m);
    }

    [Fact]
    public void UltimaAtualizacao_DeveSerAtualizadaQuandoAdicionarCredito()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);
        var dataInicial = consolidado.UltimaAtualizacao;
        Thread.Sleep(10);

        // Act
        consolidado.AdicionarCredito(100m);

        // Assert
        consolidado.UltimaAtualizacao.Should().BeAfter(dataInicial);
    }

    [Fact]
    public void UltimaAtualizacao_DeveSerAtualizadaQuandoAdicionarDebito()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);
        var dataInicial = consolidado.UltimaAtualizacao;
        Thread.Sleep(10);

        // Act
        consolidado.AdicionarDebito(50m);

        // Assert
        consolidado.UltimaAtualizacao.Should().BeAfter(dataInicial);
    }

    [Fact]
    public void SaldoLiquido_DeveSerNegativo_QuandoDebitosSuperioresCreditos()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);

        // Act
        consolidado.AdicionarCredito(100m);
        consolidado.AdicionarDebito(200m);

        // Assert
        consolidado.SaldoLiquido.Should().Be(-100m);
    }

    [Fact]
    public void SaldoLiquido_DeveSerPositivo_QuandoCreditosSuperioresDebitos()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);

        // Act
        consolidado.AdicionarCredito(300m);
        consolidado.AdicionarDebito(150m);

        // Assert
        consolidado.SaldoLiquido.Should().Be(150m);
    }

    [Fact]
    public void SaldoLiquido_DeveSerZero_QuandoCreditosIguaisDebitos()
    {
        // Arrange
        var consolidado = new Shared.Domain.Entities.Consolidado("Comerciante", DateTime.Now);

        // Act
        consolidado.AdicionarCredito(100m);
        consolidado.AdicionarDebito(100m);

        // Assert
        consolidado.SaldoLiquido.Should().Be(0m);
    }
}