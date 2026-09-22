using MediatR;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Consultas.ConsultarSaldo;

public class ConsultarSaldoManipulador(IContaRepositorio repositorio)
    : IRequestHandler<ConsultarSaldoConsulta, SaldoResposta>
{
    public async Task<SaldoResposta> Handle(ConsultarSaldoConsulta request, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterPorIdAsync(request.ContaId, cancellationToken)
            ?? throw new ContaNaoEncontradaException(request.ContaId);

        return new SaldoResposta(conta.SaldoAtual, DateTime.UtcNow);
    }
}
