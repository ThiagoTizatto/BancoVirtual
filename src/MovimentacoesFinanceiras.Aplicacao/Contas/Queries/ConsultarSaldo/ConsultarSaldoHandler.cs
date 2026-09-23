using MediatR;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ConsultarSaldo;

public class ConsultarSaldoHandler(IContaRepository repositorio)
    : IRequestHandler<ConsultarSaldoQuery, SaldoResponse>
{
    public async Task<SaldoResponse> Handle(ConsultarSaldoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conta = await repositorio.ObterPorIdAsync(request.ContaId, cancellationToken)
            ?? throw new ContaNaoEncontradaException(request.ContaId);

        return new SaldoResponse(conta.SaldoAtual.Quantia, DateTime.UtcNow);
    }
}
