using MediatR;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ConsultarSaldoEm;

public record ConsultarSaldoEmQuery(Guid ContaId, DateTime DataReferencia) : IRequest<SaldoHistoricoResponse>;
