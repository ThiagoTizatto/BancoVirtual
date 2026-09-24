using System.Text.Json;
using FluentAssertions;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Produtor.RabbitMq;
using NSubstitute;
using RabbitMQ.Client;

namespace Produtor.Testes.RabbitMq;

public class PublicadorRabbitMqTestes
{
    private readonly IConexaoRabbitMq _conexao = Substitute.For<IConexaoRabbitMq>();
    private readonly IChannel _canal = Substitute.For<IChannel>();

    public PublicadorRabbitMqTestes()
    {
        _conexao.CriarCanalAsync(Arg.Any<CancellationToken>()).Returns(_canal);
    }

    private PublicadorRabbitMq CriarPublicador() => new(_conexao);

    [Fact]
    public async Task PublicarAsync_MensagemValida_PublicaNoExchangeCorreto()
    {
        // Arrange
        var publicador = CriarPublicador();
        var mensagem = new ComandoMovimentacaoMessage(
            ContaId: Guid.NewGuid(),
            Tipo: TipoLancamento.Credito,
            Valor: 200m,
            Descricao: "Depósito",
            ChaveIdempotencia: Guid.NewGuid().ToString());

        // Act
        await publicador.PublicarAsync(mensagem, CancellationToken.None);

        // Assert
        await _canal.Received(1).BasicPublishAsync(
            exchange: TopologiaRabbitMq.Exchange,
            routingKey: TopologiaRabbitMq.RoutingKey,
            mandatory: false,
            basicProperties: Arg.Is<BasicProperties>(p => p.Persistent && p.MessageId == mensagem.ChaveIdempotencia),
            body: Arg.Is<ReadOnlyMemory<byte>>(b => CorpoContemMensagem(b, mensagem)),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublicarAsync_SemChaveIdempotencia_NaoDefineMesageId()
    {
        // Arrange
        var publicador = CriarPublicador();
        var mensagem = new ComandoMovimentacaoMessage(
            Guid.NewGuid(), TipoLancamento.Debito, 50m, null, null);

        // Act
        await publicador.PublicarAsync(mensagem, CancellationToken.None);

        // Assert
        await _canal.Received(1).BasicPublishAsync(
            exchange: Arg.Any<string>(),
            routingKey: Arg.Any<string>(),
            mandatory: Arg.Any<bool>(),
            basicProperties: Arg.Is<BasicProperties>(p => p.Persistent && p.MessageId == null),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    private static bool CorpoContemMensagem(ReadOnlyMemory<byte> corpo, ComandoMovimentacaoMessage esperado)
    {
        var deserializado = JsonSerializer.Deserialize<ComandoMovimentacaoMessage>(corpo.Span);
        return deserializado == esperado;
    }
}
