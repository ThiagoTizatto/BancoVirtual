using FluentAssertions;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace Dominio.Testes.Contas;

public class ContaTestes
{
    [Fact]
    public void Criar_DeveInicializarComSaldoZero()
    {
        // Act
        var conta = Conta.Criar(Guid.NewGuid());

        // Assert
        conta.SaldoAtual.Quantia.Should().Be(0m);
        conta.Lancamentos.Should().BeEmpty();
    }

    [Fact]
    public void Creditar_DeveAumentarSaldo()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());

        // Act
        conta.Creditar(Dinheiro.De(100m), "Depósito");

        // Assert
        conta.SaldoAtual.Quantia.Should().Be(100m);
    }

    [Fact]
    public void Creditar_DeveRegistrarLancamentoDoTipoCorreto()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());

        // Act
        conta.Creditar(Dinheiro.De(100m), "Depósito");

        // Assert
        conta.Lancamentos.Should().HaveCount(1);
        conta.Lancamentos[0].Tipo.Should().Be(TipoLancamento.Credito);
        conta.Lancamentos[0].Valor.Should().Be(100m);
    }

    [Fact]
    public void Debitar_ComSaldoSuficiente_DeveReduzirSaldo()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(200m), "Depósito");

        // Act
        conta.Debitar(Dinheiro.De(80m), "Saque");

        // Assert
        conta.SaldoAtual.Quantia.Should().Be(120m);
    }

    [Fact]
    public void Debitar_DeveRegistrarLancamentoDoTipoCorreto()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(200m), "Depósito");

        // Act
        conta.Debitar(Dinheiro.De(50m), "Saque");

        // Assert
        conta.Lancamentos[1].Tipo.Should().Be(TipoLancamento.Debito);
        conta.Lancamentos[1].Valor.Should().Be(50m);
    }

    [Fact]
    public void Debitar_ComSaldoInsuficiente_LancaSaldoInsuficienteException()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(50m), "Depósito");
        var acao = () => conta.Debitar(Dinheiro.De(100m), "Saque");

        // Act & Assert
        acao.Should().Throw<SaldoInsuficienteException>()
            .Which.SaldoAtual.Should().Be(50m);
    }

    [Fact]
    public void Debitar_ComContaSemSaldo_LancaSaldoInsuficienteException()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());
        var acao = () => conta.Debitar(Dinheiro.De(1m), "Saque");

        // Act & Assert
        acao.Should().Throw<SaldoInsuficienteException>();
    }

    [Fact]
    public void Creditar_DeveAtualizarVersaoLinha()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());
        var versaoAnterior = conta.VersaoLinha;

        // Act
        conta.Creditar(Dinheiro.De(100m), "Depósito");

        // Assert
        conta.VersaoLinha.Should().NotBe(versaoAnterior);
    }

    [Fact]
    public void Debitar_DeveAtualizarVersaoLinha()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(100m), "Depósito");
        var versaoAnterior = conta.VersaoLinha;

        // Act
        conta.Debitar(Dinheiro.De(50m), "Saque");

        // Assert
        conta.VersaoLinha.Should().NotBe(versaoAnterior);
    }

    [Fact]
    public void Debitar_ComSaldoInsuficiente_ExcecaoContemValorSolicitado()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(50m), "Depósito");
        var acao = () => conta.Debitar(Dinheiro.De(100m), "Saque");

        // Act & Assert
        acao.Should().Throw<SaldoInsuficienteException>()
            .Which.ValorSolicitado!.Quantia.Should().Be(100m);
    }

    [Fact]
    public void MultiplosLancamentos_SaldoDeveSerConsistente()
    {
        // Arrange
        var conta = Conta.Criar(Guid.NewGuid());

        // Act
        conta.Creditar(Dinheiro.De(500m), "Salário");
        conta.Debitar(Dinheiro.De(100m), "Aluguel");
        conta.Debitar(Dinheiro.De(50m), "Mercado");
        conta.Creditar(Dinheiro.De(200m), "Bônus");

        // Assert
        conta.SaldoAtual.Quantia.Should().Be(550m);
        conta.Lancamentos.Should().HaveCount(4);
    }
}
