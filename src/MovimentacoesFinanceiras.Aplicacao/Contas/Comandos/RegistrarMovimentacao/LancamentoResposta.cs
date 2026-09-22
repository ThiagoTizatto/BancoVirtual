using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Comandos.RegistrarMovimentacao;

public record LancamentoResposta(
    Guid Id,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    DateTime CriadoEm
);
