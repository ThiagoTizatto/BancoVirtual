namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.CriarConta;

public record ContaResponse(Guid Id, Guid ClienteId, decimal SaldoAtual, DateTime CriadoEm);
