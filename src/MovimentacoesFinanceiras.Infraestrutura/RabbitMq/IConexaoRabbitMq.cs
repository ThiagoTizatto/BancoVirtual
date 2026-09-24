using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Infraestrutura.RabbitMq;

public interface IConexaoRabbitMq : IAsyncDisposable
{
    Task<IChannel> CriarCanalAsync(CancellationToken ct = default);
}
