using System.Text.Json;
using MovimentacoesFinanceiras.Mensagens;
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Produtor.RabbitMq;

public sealed class PublicadorRabbitMq : IPublicadorDeMovimentacoes
{
    private readonly IConexaoRabbitMq _conexao;

    public PublicadorRabbitMq(IConexaoRabbitMq conexao)
    {
        _conexao = conexao;
    }

    public async Task PublicarAsync(ComandoMovimentacaoMessage mensagem, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mensagem);
        await using var canal = await _conexao.CriarCanalAsync(ct);

        var body = JsonSerializer.SerializeToUtf8Bytes(mensagem);

        var props = new BasicProperties
        {
            Persistent = true,
            MessageId = mensagem.ChaveIdempotencia,
        };

        await canal.BasicPublishAsync(
            exchange: TopologiaRabbitMq.Exchange,
            routingKey: TopologiaRabbitMq.RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}
