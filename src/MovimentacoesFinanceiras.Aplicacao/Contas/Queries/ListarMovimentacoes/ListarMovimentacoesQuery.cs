using MediatR;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ListarMovimentacoes;

public record ListarMovimentacoesQuery(Guid ContaId, int Pagina, int TamanhoPagina)
    : IRequest<ExtratoResponse>;
