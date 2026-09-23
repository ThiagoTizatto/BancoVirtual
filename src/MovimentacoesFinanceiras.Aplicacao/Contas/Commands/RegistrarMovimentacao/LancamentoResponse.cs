using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;

public record LancamentoResponse(
    Guid Id,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    DateTime CriadoEm
);
