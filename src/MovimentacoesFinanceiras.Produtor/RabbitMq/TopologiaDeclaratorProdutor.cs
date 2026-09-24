using MovimentacoesFinanceiras.Mensagens;
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Produtor.RabbitMq;

public sealed class TopologiaDeclaratorProdutor
{
    private readonly IConexaoRabbitMq _conexao;

    public TopologiaDeclaratorProdutor(IConexaoRabbitMq conexao)
    {
        _conexao = conexao;
    }

    public async Task DeclararAsync(CancellationToken ct = default)
    {
        await using var canal = await _conexao.CriarCanalAsync(ct);
        await canal.ExchangeDeclareAsync(
            exchange: TopologiaRabbitMq.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            cancellationToken: ct);
    }
}
