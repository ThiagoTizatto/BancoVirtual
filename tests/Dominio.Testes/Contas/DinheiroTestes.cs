using FluentAssertions;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace Dominio.Testes.Contas;

public class DinheiroTestes
{
    [Fact]
    public void De_QuandoValorPositivo_CriaDinheiro()
    {
        // Act
        var dinheiro = Dinheiro.De(100m);

        // Assert
        dinheiro.Quantia.Should().Be(100m);
    }

    [Fact]
    public void De_QuandoValorZero_LancaArgumentException()
    {
        // Arrange
        var acao = () => Dinheiro.De(0m);

        // Act & Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*deve ser maior que zero*");
    }

    [Fact]
    public void De_QuandoValorNegativo_LancaArgumentException()
    {
        // Arrange
        var acao = () => Dinheiro.De(-50m);

        // Act & Assert
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*deve ser maior que zero*");
    }

    [Fact]
    public void Igualdade_QuandoMesmaQuantia_SaoIguais()
    {
        // Act
        var iguais = Dinheiro.De(100m).Equals(Dinheiro.De(100m));

        // Assert
        iguais.Should().BeTrue();
    }

    [Fact]
    public void ToString_RetornaFormatoMonetarioPtBr()
    {
        // Arrange
        var dinheiro = Dinheiro.De(1500.50m);

        // Act
        var texto = dinheiro.ToString();

        // Assert
        texto.Should().Be("R$ 1.500,50");
    }

    [Fact]
    public void Igualdade_QuandoQuantiasDiferentes_NaoSaoIguais()
    {
        // Act
        var iguais = Dinheiro.De(100m).Equals(Dinheiro.De(200m));

        // Assert
        iguais.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_QuandoMesmaQuantia_MesmoHash()
    {
        // Act
        var hashA = Dinheiro.De(100m).GetHashCode();
        var hashB = Dinheiro.De(100m).GetHashCode();

        // Assert
        hashA.Should().Be(hashB);
    }
}
