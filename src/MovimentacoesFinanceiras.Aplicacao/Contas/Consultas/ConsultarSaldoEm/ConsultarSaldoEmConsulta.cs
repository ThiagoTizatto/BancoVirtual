using MediatR;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Consultas.ConsultarSaldoEm;

public record ConsultarSaldoEmConsulta(Guid ContaId, DateTime DataReferencia) : IRequest<SaldoHistoricoResposta>;
