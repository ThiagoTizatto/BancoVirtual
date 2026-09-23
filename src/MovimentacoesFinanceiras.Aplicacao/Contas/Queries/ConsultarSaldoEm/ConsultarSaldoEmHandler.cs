using MediatR;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ConsultarSaldoEm;

public class ConsultarSaldoEmHandler(IContaRepository repositorio)
    : IRequestHandler<ConsultarSaldoEmQuery, SaldoHistoricoResponse>
{
    public async Task<SaldoHistoricoResponse> Handle(ConsultarSaldoEmQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conta = await repositorio.ObterPorIdAsync(request.ContaId, cancellationToken)
            ?? throw new ContaNaoEncontradaException(request.ContaId);

        var saldo = await repositorio.ConsultarSaldoEmAsync(request.ContaId, request.DataReferencia, cancellationToken);

        return new SaldoHistoricoResponse(saldo, request.DataReferencia);
    }
}
