using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Produtor.RabbitMq;

public sealed partial class ConexaoRabbitMq : IConexaoRabbitMq
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ConexaoRabbitMq> _logger;
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConexaoRabbitMq(IOptions<RabbitMqOptions> options, ILogger<ConexaoRabbitMq> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IChannel> CriarCanalAsync(CancellationToken ct = default)
    {
        var connection = await ObterConexaoAsync(ct);
        return await connection.CreateChannelAsync(cancellationToken: ct);
    }

    private async Task<IConnection> ObterConexaoAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
            };

            _connection = await factory.CreateConnectionAsync(ct);
            LogConexaoEstabelecida(_options.Host, _options.Port);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();

        _lock.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Conexão RabbitMQ estabelecida: {Host}:{Port}")]
    private partial void LogConexaoEstabelecida(string host, int port);
}
