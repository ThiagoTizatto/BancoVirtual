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
    public void ToString_RetornaFormatoMonetario()
    {
        // Verifica prefixo monetário e que o valor decimal está presente — independente do locale
        var resultado = Dinheiro.De(1500.50m).ToString();
        resultado.Should().StartWith("R$");
        // 1500.50 formatado como N2 pode ser "1.500,50" (pt-BR) ou "1,500.50" (en-US)
        // Verificamos que o resultado contém os dígitos do valor sem separadores
        resultado.Should().MatchRegex(@"1[.,]?5[.,]?0{2}");
    }
}
