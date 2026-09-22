using MediatR;
using Microsoft.EntityFrameworkCore;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;
using Polly;
using Polly.Retry;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Comandos.RegistrarMovimentacao;

public class RegistrarMovimentacaoManipulador(IContaRepositorio repositorio)
    : IRequestHandler<RegistrarMovimentacaoComando, LancamentoResposta>
{
    private static readonly AsyncRetryPolicy PoliticaRetentativa = Policy
        .Handle<DbUpdateConcurrencyException>()
        .WaitAndRetryAsync(3, tentativa => TimeSpan.FromMilliseconds(50 * Math.Pow(2, tentativa)));

    public async Task<LancamentoResposta> Handle(RegistrarMovimentacaoComando request, CancellationToken cancellationToken)
    {
        if (request.ChaveIdempotencia is not null)
        {
            var lancamentoExistente = await repositorio.ObterLancamentoPorChaveIdempotenciaAsync(
                request.ChaveIdempotencia, cancellationToken);

            if (lancamentoExistente is not null)
                return ToResposta(lancamentoExistente);
        }

        return await PoliticaRetentativa.ExecuteAsync(async () =>
        {
            var conta = await repositorio.ObterPorIdAsync(request.ContaId, cancellationToken)
                ?? throw new ContaNaoEncontradaException(request.ContaId);

            var dinheiro = Dinheiro.De(request.Valor);

            var lancamento = request.Tipo == TipoLancamento.Credito
                ? conta.Creditar(dinheiro, request.Descricao, request.ChaveIdempotencia)
                : conta.Debitar(dinheiro, request.Descricao, request.ChaveIdempotencia);

            await repositorio.SalvarAsync(cancellationToken);
            return ToResposta(lancamento);
        });
    }

    private static LancamentoResposta ToResposta(Lancamento lancamento) =>
        new(lancamento.Id, lancamento.Tipo, lancamento.Valor, lancamento.Descricao, lancamento.CriadoEm);
}
