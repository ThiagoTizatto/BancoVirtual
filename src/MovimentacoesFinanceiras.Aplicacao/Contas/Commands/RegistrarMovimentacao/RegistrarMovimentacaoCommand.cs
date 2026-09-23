using MediatR;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;

public record RegistrarMovimentacaoCommand(
    Guid ContaId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    string? ChaveIdempotencia
) : IRequest<LancamentoResponse>;
