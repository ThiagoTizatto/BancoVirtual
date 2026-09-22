using MediatR;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Consultas.ConsultarSaldo;

public record ConsultarSaldoConsulta(Guid ContaId) : IRequest<SaldoResposta>;
