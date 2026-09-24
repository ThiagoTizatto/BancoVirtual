using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Worker.Processamento;
using NSubstitute;

namespace Worker.Testes.Processamento;

public class ProcessadorDeMovimentacaoTestes
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private ProcessadorDeMovimentacao CriarProcessador() =>
        new(_mediator, NullLogger<ProcessadorDeMovimentacao>.Instance);

    [Fact]
    public async Task ProcessarAsync_MensagemValida_EnviaCommandCorretoAoMediador()
    {
        // Arrange
        var processador = CriarProcessador();
        var mensagem = new ComandoMovimentacaoMessage(
            ContaId: Guid.NewGuid(),
            Tipo: TipoLancamento.Credito,
            Valor: 150m,
            Descricao: "Depósito",
            ChaveIdempotencia: Guid.NewGuid().ToString());

        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem);

        // Act
        await processador.ProcessarAsync(corpo, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<RegistrarMovimentacaoCommand>(c =>
                c.ContaId == mensagem.ContaId &&
                c.Tipo == TipoLancamento.Credito &&
                c.Valor == 150m &&
                c.ChaveIdempotencia == mensagem.ChaveIdempotencia),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessarAsync_ErroNegocio_PropagaExcecaoSemRetry()
    {
        // Arrange
        var processador = CriarProcessador();
        _mediator
            .Send(Arg.Any<RegistrarMovimentacaoCommand>(), Arg.Any<CancellationToken>())
            .Returns<LancamentoResponse>(_ => throw new SaldoInsuficienteException(0m, Dinheiro.De(100m)));

        var mensagem = new ComandoMovimentacaoMessage(
            Guid.NewGuid(), TipoLancamento.Debito, 100m, null, null);
        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem);

        // Act
        var acao = () => processador.ProcessarAsync(corpo, CancellationToken.None);

        // Assert
        await acao.Should().ThrowAsync<SaldoInsuficienteException>();
        await _mediator.Received(1).Send(
            Arg.Any<RegistrarMovimentacaoCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessarAsync_CorpoInvalido_LancaException()
    {
        // Arrange
        var processador = CriarProcessador();
        var corpoInvalido = "nao-e-json"u8.ToArray();

        // Act
        var acao = () => processador.ProcessarAsync(corpoInvalido, CancellationToken.None);

        // Assert
        await acao.Should().ThrowAsync<Exception>();
    }
}
