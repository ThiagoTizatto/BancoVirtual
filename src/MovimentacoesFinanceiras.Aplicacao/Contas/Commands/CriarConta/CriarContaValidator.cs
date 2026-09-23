using FluentValidation;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.CriarConta;

public class CriarContaValidator : AbstractValidator<CriarContaCommand>
{
    public CriarContaValidator()
    {
        RuleFor(c => c.ClienteId).NotEmpty();
    }
}
