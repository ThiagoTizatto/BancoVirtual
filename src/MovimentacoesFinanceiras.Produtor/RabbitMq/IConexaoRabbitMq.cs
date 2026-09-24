using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Produtor.RabbitMq;

public interface IConexaoRabbitMq : IAsyncDisposable
{
    Task<IChannel> CriarCanalAsync(CancellationToken ct = default);
}
