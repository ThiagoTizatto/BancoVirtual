using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Mensagens;

namespace MovimentacoesFinanceiras.Infraestrutura.Contas.RabbitMq;

public sealed partial class ProcessadorDeMovimentacao
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessadorDeMovimentacao> _logger;

    public ProcessadorDeMovimentacao(IMediator mediator, ILogger<ProcessadorDeMovimentacao> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task ProcessarAsync(byte[] corpo, CancellationToken ct)
    {
        var mensagem = JsonSerializer.Deserialize<ComandoMovimentacaoMessage>(corpo)
            ?? throw new InvalidOperationException("Corpo da mensagem deserializou para null.");

        LogProcessando(mensagem.ContaId, mensagem.Tipo, mensagem.Valor);

        var command = new RegistrarMovimentacaoCommand(
            mensagem.ContaId,
            mensagem.Tipo,
            mensagem.Valor,
            mensagem.Descricao,
            mensagem.ChaveIdempotencia);

        await _mediator.Send(command, ct);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Processando movimentação: ContaId={ContaId} Tipo={Tipo} Valor={Valor}")]
    private partial void LogProcessando(Guid contaId, TipoLancamento tipo, decimal valor);
}
