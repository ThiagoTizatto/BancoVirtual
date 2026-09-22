using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovimentacoesFinanceiras.Aplicacao.Contas.Comandos.RegistrarMovimentacao;
using MovimentacoesFinanceiras.Aplicacao.Contas.Consultas.ConsultarSaldo;
using MovimentacoesFinanceiras.Aplicacao.Contas.Consultas.ConsultarSaldoEm;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;

namespace MovimentacoesFinanceiras.Api.Controladores;

[ApiController]
[Route("contas")]
[Produces("application/json")]
public class ContasControlador(IMediator mediador, ContextoBancoDados contexto, IContaRepositorio repositorio) : ControllerBase
{
    /// <summary>Cria uma nova conta para um cliente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> CriarConta(
        [FromBody] CriarContaRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        var conta = Conta.Criar(requisicao.ClienteId);
        await contexto.Contas.AddAsync(conta, cancellationToken);
        await contexto.SaveChangesAsync(cancellationToken);

        var resposta = new
        {
            id = conta.Id,
            clienteId = conta.ClienteId,
            saldoAtual = conta.SaldoAtual,
            criadoEm = conta.CriadoEm
        };

        return CreatedAtAction(nameof(ConsultarSaldo), new { id = conta.Id }, resposta);
    }

    /// <summary>Registra uma movimentação financeira (crédito ou débito).</summary>
    [HttpPost("{id:guid}/movimentacoes")]
    [ProducesResponseType(typeof(LancamentoResposta), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegistrarMovimentacao(
        Guid id,
        [FromBody] RegistrarMovimentacaoRequisicao requisicao,
        [FromHeader(Name = "Idempotency-Key")] string? chaveIdempotencia,
        CancellationToken cancellationToken)
    {
        var comando = new RegistrarMovimentacaoComando(id, requisicao.Tipo, requisicao.Valor, requisicao.Descricao, chaveIdempotencia);
        var resposta = await mediador.Send(comando, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, resposta);
    }

    /// <summary>
    /// Consulta o saldo da conta.
    /// Sem o parâmetro 'em': retorna o saldo atual (O(1)).
    /// Com o parâmetro 'em': retorna o saldo no ponto no tempo informado.
    /// </summary>
    [HttpGet("{id:guid}/saldo")]
    [ProducesResponseType(typeof(SaldoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SaldoHistoricoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarSaldo(
        Guid id,
        [FromQuery] DateTime? em,
        CancellationToken cancellationToken)
    {
        if (em.HasValue)
        {
            var consulta = new ConsultarSaldoEmConsulta(id, em.Value.ToUniversalTime());
            var resposta = await mediador.Send(consulta, cancellationToken);
            return Ok(resposta);
        }
        else
        {
            var consulta = new ConsultarSaldoConsulta(id);
            var resposta = await mediador.Send(consulta, cancellationToken);
            return Ok(resposta);
        }
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

public record CriarContaRequisicao(Guid ClienteId);
public record RegistrarMovimentacaoRequisicao(TipoLancamento Tipo, decimal Valor, string? Descricao);
