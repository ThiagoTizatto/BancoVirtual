using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;
using Polly;
using Polly.Retry;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;

public class RegistrarMovimentacaoHandler(IContaRepository repositorio, IServiceScopeFactory escopoFactory)
    : IRequestHandler<RegistrarMovimentacaoCommand, LancamentoResponse>
{
    private static readonly AsyncRetryPolicy PoliticaRetentativa = Policy
        .Handle<DbUpdateConcurrencyException>()
        .WaitAndRetryAsync(3, tentativa => TimeSpan.FromMilliseconds(50 * Math.Pow(2, tentativa)));

    public async Task<LancamentoResponse> Handle(RegistrarMovimentacaoCommand request, CancellationToken cancellationToken)
    {
        // Verifica idempotência antes de qualquer operação de escrita
        if (request.ChaveIdempotencia is not null)
        {
            var lancamentoExistente = await repositorio.ObterLancamentoPorChaveIdempotenciaAsync(
                request.ChaveIdempotencia, cancellationToken);

            if (lancamentoExistente is not null)
                return ToResponse(lancamentoExistente);
        }

        // Cada tentativa do Polly usa um novo escopo de DI (e portanto um novo DbContext)
        // para garantir que o contexto esteja limpo após DbUpdateConcurrencyException
        return await PoliticaRetentativa.ExecuteAsync(async () =>
        {
            await using var escopo = escopoFactory.CreateAsyncScope();
            var repo = escopo.ServiceProvider.GetRequiredService<IContaRepository>();

            var conta = await repo.ObterPorIdAsync(request.ContaId, cancellationToken)
                ?? throw new ContaNaoEncontradaException(request.ContaId);

            var dinheiro = Dinheiro.De(request.Valor);

            var lancamento = request.Tipo == TipoLancamento.Credito
                ? conta.Creditar(dinheiro, request.Descricao, request.ChaveIdempotencia)
                : conta.Debitar(dinheiro, request.Descricao, request.ChaveIdempotencia);

            // Adiciona explicitamente o lançamento ao contexto para garantir INSERT
            // (o tracking automático via coleção de navegação não funciona corretamente
            // com backing fields quando a conta é carregada sem Include)
            await repo.AdicionarLancamentoAsync(lancamento, cancellationToken);
            await repo.SalvarAsync(cancellationToken);
            return ToResponse(lancamento);
        });
    }

    private static LancamentoResponse ToResponse(Lancamento lancamento) =>
        new(lancamento.Id, lancamento.Tipo, lancamento.Valor, lancamento.Descricao, lancamento.CriadoEm);
}
