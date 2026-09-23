using MediatR;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Queries.ConsultarSaldo;

public record ConsultarSaldoQuery(Guid ContaId) : IRequest<SaldoResponse>;
