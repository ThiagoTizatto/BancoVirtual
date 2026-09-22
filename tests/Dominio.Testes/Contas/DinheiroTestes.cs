using FluentAssertions;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace Dominio.Testes.Contas;

public class DinheiroTestes
{
    [Fact]
    public void De_QuandoValorPositivo_CriaDinheiro()
    {
        var dinheiro = Dinheiro.De(100m);
        dinheiro.Quantia.Should().Be(100m);
    }

    [Fact]
    public void De_QuandoValorZero_LancaArgumentException()
    {
        var acao = () => Dinheiro.De(0m);
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*deve ser maior que zero*");
    }

    [Fact]
    public void De_QuandoValorNegativo_LancaArgumentException()
    {
        var acao = () => Dinheiro.De(-50m);
        acao.Should().Throw<ArgumentException>()
            .WithMessage("*deve ser maior que zero*");
    }

    [Fact]
    public void Igualdade_QuandoMesmaQuantia_SaoIguais()
    {
        Dinheiro.De(100m).Should().Be(Dinheiro.De(100m));
    }

    [Fact]
    public void ToString_RetornaFormatoMonetarioPtBr()
    {
        Dinheiro.De(1500.50m).ToString().Should().Be("R$ 1.500,50");
    }

    [Fact]
    public void Igualdade_QuandoQuantiasDiferentes_NaoSaoIguais()
    {
        Dinheiro.De(100m).Should().NotBe(Dinheiro.De(200m));
    }

    [Fact]
    public void GetHashCode_QuandoMesmaQuantia_MesmoHash()
    {
        Dinheiro.De(100m).GetHashCode().Should().Be(Dinheiro.De(100m).GetHashCode());
    }
}
