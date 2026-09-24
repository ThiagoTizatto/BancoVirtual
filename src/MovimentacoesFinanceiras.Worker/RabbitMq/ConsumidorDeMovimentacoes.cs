using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Worker.Processamento;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MovimentacoesFinanceiras.Worker.RabbitMq;

public sealed partial class ConsumidorDeMovimentacoes : BackgroundService
{
    private readonly IConexaoRabbitMq _conexao;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConsumidorDeMovimentacoes> _logger;

    public ConsumidorDeMovimentacoes(
        IConexaoRabbitMq conexao,
        IServiceScopeFactory scopeFactory,
        ILogger<ConsumidorDeMovimentacoes> logger)
    {
        _conexao = conexao;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var canal = await _conexao.CriarCanalAsync(stoppingToken);

        await canal.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(canal);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var deliveryTag = ea.DeliveryTag;
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processador = scope.ServiceProvider.GetRequiredService<ProcessadorDeMovimentacao>();
                await processador.ProcessarAsync(ea.Body.ToArray(), stoppingToken);

                await canal.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                LogErroAoProcessar(ex, deliveryTag);
                await canal.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: CancellationToken.None);
            }
        };

        await canal.BasicConsumeAsync(
            queue: TopologiaRabbitMq.FilaPrincipal,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Falha ao processar mensagem (DeliveryTag={DeliveryTag}). Enviando para DLQ.")]
    private partial void LogErroAoProcessar(Exception ex, ulong deliveryTag);
}
