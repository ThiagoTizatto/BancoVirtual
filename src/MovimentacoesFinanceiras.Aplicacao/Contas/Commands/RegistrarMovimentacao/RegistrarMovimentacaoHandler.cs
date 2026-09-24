using MediatR;
using MovimentacoesFinanceiras.Aplicacao.Metricas;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;

public class RegistrarMovimentacaoHandler(
    IContaRepository repositorio,
    MetricasMovimentacao metricas)
    : IRequestHandler<RegistrarMovimentacaoCommand, LancamentoResponse>
{
    public async Task<LancamentoResponse> Handle(RegistrarMovimentacaoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ChaveIdempotencia is not null)
        {
            var lancamentoExistente = await repositorio.ObterLancamentoPorChaveIdempotenciaAsync(
                request.ChaveIdempotencia, cancellationToken);

            if (lancamentoExistente is not null)
                return ToResponse(lancamentoExistente);
        }

        var dinheiro = Dinheiro.De(request.Valor);

        var lancamento = await PoliticasResiliencia.Combinada.ExecuteAsync(
            () => repositorio.RegistrarMovimentacaoAsync(
                request.ContaId, 
                request.Tipo,
                dinheiro, 
                request.Descricao,
                request.ChaveIdempotencia, 
                cancellationToken));

        metricas.RegistrarMovimentacao(request.Tipo.ToString());

        return ToResponse(lancamento);
    }

    private static LancamentoResponse ToResponse(Lancamento lancamento) =>
        new(lancamento.Id, lancamento.Tipo, lancamento.Valor, lancamento.Descricao, lancamento.CriadoEm);
}
