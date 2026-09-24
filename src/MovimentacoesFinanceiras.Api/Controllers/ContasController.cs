using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MovimentacoesFinanceiras.Aplicacao.Contas.Commands.CriarConta;
using MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;
using MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ConsultarSaldo;
using MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ConsultarSaldoEm;
using MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ListarMovimentacoes;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Api.Controllers;

[ApiController]
[Route("contas")]
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("por-api-key")]
public class ContasController(IMediator mediador) : ControllerBase
{
    /// <summary>Cria uma nova conta para um cliente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContaResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CriarConta(
        [FromBody] CriarContaRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await mediador.Send(new CriarContaCommand(request.ClienteId), cancellationToken);
        return CreatedAtAction(nameof(ConsultarSaldo), new { id = response.Id }, response);
    }

    /// <summary>Registra uma movimentação financeira (crédito ou débito).</summary>
    [HttpPost("{id:guid}/movimentacoes")]
    [ProducesResponseType(typeof(LancamentoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistrarMovimentacao(
        Guid id,
        [FromBody] RegistrarMovimentacaoRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? chaveIdempotencia,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RegistrarMovimentacaoCommand(id, request.Tipo, request.Valor, request.Descricao, chaveIdempotencia);
        var response = await mediador.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Consulta o saldo da conta. Sem 'em': saldo atual (O(1)). Com 'em': saldo point-in-time.
    /// </summary>
    [HttpGet("{id:guid}/saldo")]
    [ProducesResponseType(typeof(SaldoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SaldoHistoricoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarSaldo(
        Guid id,
        [FromQuery] DateTime? em,
        CancellationToken cancellationToken)
    {
        if (em.HasValue)
        {
            var queryHistorico = new ConsultarSaldoEmQuery(id, em.Value.ToUniversalTime());
            return Ok(await mediador.Send(queryHistorico, cancellationToken));
        }

        return Ok(await mediador.Send(new ConsultarSaldoQuery(id), cancellationToken));
    }

    /// <summary>Lista o extrato paginado da conta.</summary>
    [HttpGet("{id:guid}/movimentacoes")]
    [ProducesResponseType(typeof(ExtratoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarMovimentacoes(
        Guid id,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ListarMovimentacoesQuery(id, pagina, tamanhoPagina);
        return Ok(await mediador.Send(query, cancellationToken));
    }
}

public record CriarContaRequest(Guid ClienteId);
public record RegistrarMovimentacaoRequest(TipoLancamento Tipo, decimal Valor, string? Descricao);
