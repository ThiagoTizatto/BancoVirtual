namespace MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ListarMovimentacoes;

public record ItemExtrato(Guid Id, string Tipo, decimal Valor, string? Descricao, DateTime CriadoEm);

public record ExtratoResponse(IReadOnlyList<ItemExtrato> Itens, int Pagina, int TamanhoPagina, int Total);
