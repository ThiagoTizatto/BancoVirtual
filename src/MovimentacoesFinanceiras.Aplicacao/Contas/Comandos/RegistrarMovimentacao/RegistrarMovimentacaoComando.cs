using MediatR;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Comandos.RegistrarMovimentacao;

public record RegistrarMovimentacaoComando(
    Guid ContaId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    string? ChaveIdempotencia
) : IRequest<LancamentoResposta>;
