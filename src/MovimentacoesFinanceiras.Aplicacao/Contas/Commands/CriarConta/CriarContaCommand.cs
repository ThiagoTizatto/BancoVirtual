using MediatR;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.CriarConta;

public record CriarContaCommand(Guid ClienteId) : IRequest<ContaResponse>;
