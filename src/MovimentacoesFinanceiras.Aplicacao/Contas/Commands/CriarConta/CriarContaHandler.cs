using MediatR;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.CriarConta;

public class CriarContaHandler(IContaRepository repositorio)
    : IRequestHandler<CriarContaCommand, ContaResponse>
{
    public async Task<ContaResponse> Handle(CriarContaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conta = Conta.Criar(request.ClienteId);
        await repositorio.AdicionarAsync(conta, cancellationToken);
        await repositorio.SalvarAsync(cancellationToken);

        return new ContaResponse(conta.Id, conta.ClienteId, conta.SaldoAtual.Quantia, conta.CriadoEm);
    }
}
