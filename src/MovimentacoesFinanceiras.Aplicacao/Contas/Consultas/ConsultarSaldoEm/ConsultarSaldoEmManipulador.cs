using MediatR;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Consultas.ConsultarSaldoEm;

public class ConsultarSaldoEmManipulador(IContaRepositorio repositorio)
    : IRequestHandler<ConsultarSaldoEmConsulta, SaldoHistoricoResposta>
{
    public async Task<SaldoHistoricoResposta> Handle(ConsultarSaldoEmConsulta request, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterPorIdAsync(request.ContaId, cancellationToken)
            ?? throw new ContaNaoEncontradaException(request.ContaId);

        var saldo = await repositorio.ConsultarSaldoEmAsync(request.ContaId, request.DataReferencia, cancellationToken);

        return new SaldoHistoricoResposta(saldo, request.DataReferencia);
    }
}
