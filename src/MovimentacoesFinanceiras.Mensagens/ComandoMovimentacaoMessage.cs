using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Mensagens;

public record ComandoMovimentacaoMessage(
    Guid ContaId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    string? ChaveIdempotencia
);
