using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Produtor;

public record MovimentacaoRequest(
    Guid ContaId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    string? ChaveIdempotencia);
