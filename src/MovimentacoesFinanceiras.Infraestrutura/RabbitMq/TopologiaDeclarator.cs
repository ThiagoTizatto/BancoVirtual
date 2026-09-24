using MovimentacoesFinanceiras.Mensagens;
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Infraestrutura.RabbitMq;

public sealed class TopologiaDeclarator
{
    private readonly IConexaoRabbitMq _conexao;

    public TopologiaDeclarator(IConexaoRabbitMq conexao)
    {
        _conexao = conexao;
    }

    public async Task DeclararAsync(CancellationToken ct = default)
    {
        await using var canal = await _conexao.CriarCanalAsync(ct);

        await canal.ExchangeDeclareAsync(
            exchange: TopologiaRabbitMq.ExchangeDlx,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct);

        await canal.QueueDeclareAsync(
            queue: TopologiaRabbitMq.FilaDlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await canal.QueueBindAsync(
            queue: TopologiaRabbitMq.FilaDlq,
            exchange: TopologiaRabbitMq.ExchangeDlx,
            routingKey: TopologiaRabbitMq.RoutingKeyDlq,
            cancellationToken: ct);

        await canal.ExchangeDeclareAsync(
            exchange: TopologiaRabbitMq.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct);

        Dictionary<string, object?> args = new()
        {
            ["x-dead-letter-exchange"] = TopologiaRabbitMq.ExchangeDlx,
            ["x-dead-letter-routing-key"] = TopologiaRabbitMq.RoutingKeyDlq,
        };

        await canal.QueueDeclareAsync(
            queue: TopologiaRabbitMq.FilaPrincipal,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: ct);

        await canal.QueueBindAsync(
            queue: TopologiaRabbitMq.FilaPrincipal,
            exchange: TopologiaRabbitMq.Exchange,
            routingKey: TopologiaRabbitMq.RoutingKey,
            cancellationToken: ct);
    }
}
