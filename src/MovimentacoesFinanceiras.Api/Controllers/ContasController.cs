using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;
using MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ConsultarSaldo;
using MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ConsultarSaldoEm;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;

namespace MovimentacoesFinanceiras.Api.Controllers;

[ApiController]
[Route("contas")]
[Produces("application/json")]
public class ContasController(IMediator mediador, BancoDadosContext contexto, IContaRepository repositorio) : ControllerBase
{
    /// <summary>Cria uma nova conta para um cliente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> CriarConta(
        [FromBody] CriarContaRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conta = Conta.Criar(request.ClienteId);
        await contexto.Contas.AddAsync(conta, cancellationToken);
        await contexto.SaveChangesAsync(cancellationToken);

        var response = new
        {
            id = conta.Id,
            clienteId = conta.ClienteId,
            saldoAtual = conta.SaldoAtual,
            criadoEm = conta.CriadoEm
        };

        return CreatedAtAction(nameof(ConsultarSaldo), new { id = conta.Id }, response);
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
    /// Consulta o saldo da conta.
    /// Sem o parâmetro 'em': retorna o saldo atual (O(1)).
    /// Com o parâmetro 'em': retorna o saldo no ponto no tempo informado.
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

        var query = new ConsultarSaldoQuery(id);
        return Ok(await mediador.Send(query, cancellationToken));
    }

    /// <summary>Lista o extrato paginado da conta.</summary>
    [HttpGet("{id:guid}/movimentacoes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarMovimentacoes(
        Guid id,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var conta = await contexto.Contas.FindAsync([id], cancellationToken);
        if (conta is null) return NotFound();

        var (itens, total) = await repositorio.ListarLancamentosAsync(id, pagina, tamanhoPagina, cancellationToken);

        return Ok(new
        {
            itens = itens.Select(l => new
            {
                id = l.Id,
                tipo = l.Tipo.ToString(),
                valor = l.Valor,
                descricao = l.Descricao,
                criadoEm = l.CriadoEm
            }),
            pagina,
            tamanhoPagina,
            total
        });
    }
}

public record CriarContaRequest(Guid ClienteId);
public record RegistrarMovimentacaoRequest(TipoLancamento Tipo, decimal Valor, string? Descricao);
