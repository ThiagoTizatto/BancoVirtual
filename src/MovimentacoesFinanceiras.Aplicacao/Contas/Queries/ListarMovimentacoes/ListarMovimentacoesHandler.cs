using MediatR;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ListarMovimentacoes;

public class ListarMovimentacoesHandler(IContaRepository repositorio)
    : IRequestHandler<ListarMovimentacoesQuery, ExtratoResponse>
{
    public async Task<ExtratoResponse> Handle(ListarMovimentacoesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var conta = await repositorio.ObterPorIdAsync(request.ContaId, cancellationToken)
            ?? throw new ContaNaoEncontradaException(request.ContaId);

        var (itens, total) = await repositorio.ListarLancamentosAsync(
            request.ContaId, request.Pagina, request.TamanhoPagina, cancellationToken);

        var mapeados = itens
            .Select(l => new ItemExtrato(l.Id, l.Tipo.ToString(), l.Valor, l.Descricao, l.CriadoEm))
            .ToList();

        return new ExtratoResponse(mapeados, request.Pagina, request.TamanhoPagina, total);
    }
}
