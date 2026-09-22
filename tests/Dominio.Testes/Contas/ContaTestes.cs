using FluentAssertions;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace Dominio.Testes.Contas;

public class ContaTestes
{
    [Fact]
    public void Criar_DeveInicializarComSaldoZero()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        conta.SaldoAtual.Should().Be(0m);
        conta.Lancamentos.Should().BeEmpty();
    }

    [Fact]
    public void Creditar_DeveAumentarSaldo()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(100m), "Depósito");
        conta.SaldoAtual.Should().Be(100m);
    }

    [Fact]
    public void Creditar_DeveRegistrarLancamentoDoTipoCorreto()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(100m), "Depósito");
        conta.Lancamentos.Should().HaveCount(1);
        conta.Lancamentos[0].Tipo.Should().Be(TipoLancamento.Credito);
        conta.Lancamentos[0].Valor.Should().Be(100m);
    }

    [Fact]
    public void Debitar_ComSaldoSuficiente_DeveReduzirSaldo()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(200m), "Depósito");
        conta.Debitar(Dinheiro.De(80m), "Saque");
        conta.SaldoAtual.Should().Be(120m);
    }

    [Fact]
    public void Debitar_DeveRegistrarLancamentoDoTipoCorreto()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(200m), "Depósito");
        conta.Debitar(Dinheiro.De(50m), "Saque");
        conta.Lancamentos[1].Tipo.Should().Be(TipoLancamento.Debito);
        conta.Lancamentos[1].Valor.Should().Be(50m);
    }

    [Fact]
    public void Debitar_ComSaldoInsuficiente_LancaSaldoInsuficienteException()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(50m), "Depósito");
        var acao = () => conta.Debitar(Dinheiro.De(100m), "Saque");
        acao.Should().Throw<SaldoInsuficienteException>()
            .Which.SaldoAtual.Should().Be(50m);
    }

    [Fact]
    public void Debitar_ComContaSemSaldo_LancaSaldoInsuficienteException()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        var acao = () => conta.Debitar(Dinheiro.De(1m), "Saque");
        acao.Should().Throw<SaldoInsuficienteException>();
    }

    [Fact]
    public void Creditar_DeveAtualizarVersaoLinha()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        var versaoAnterior = conta.VersaoLinha;
        conta.Creditar(Dinheiro.De(100m), "Depósito");
        conta.VersaoLinha.Should().NotBe(versaoAnterior);
    }

    [Fact]
    public void MultiplosLancamentos_SaldoDeveSerConsistente()
    {
        var conta = Conta.Criar(Guid.NewGuid());
        conta.Creditar(Dinheiro.De(500m), "Salário");
        conta.Debitar(Dinheiro.De(100m), "Aluguel");
        conta.Debitar(Dinheiro.De(50m), "Mercado");
        conta.Creditar(Dinheiro.De(200m), "Bônus");
        conta.SaldoAtual.Should().Be(550m);
        conta.Lancamentos.Should().HaveCount(4);
    }
}
