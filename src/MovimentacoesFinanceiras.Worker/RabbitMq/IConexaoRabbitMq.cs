using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Worker.RabbitMq;

public interface IConexaoRabbitMq : IAsyncDisposable
{
    Task<IChannel> CriarCanalAsync(CancellationToken ct = default);
}
