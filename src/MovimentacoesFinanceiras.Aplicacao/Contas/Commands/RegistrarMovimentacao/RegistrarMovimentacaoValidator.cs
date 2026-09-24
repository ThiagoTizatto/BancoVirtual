using FluentValidation;
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;

public class RegistrarMovimentacaoValidator : AbstractValidator<RegistrarMovimentacaoCommand>
{
    public RegistrarMovimentacaoValidator()
    {
        RuleFor(x => x.ContaId)
            .NotEmpty().WithMessage("O identificador da conta é obrigatório.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("O tipo deve ser Credito ou Debito.");

        RuleFor(x => x.Descricao)
            .MaximumLength(255).WithMessage("A descrição deve ter no máximo 255 caracteres.")
            .When(x => x.Descricao is not null);

        RuleFor(x => x.ChaveIdempotencia)
            .MaximumLength(64).WithMessage("A chave de idempotência deve ter no máximo 64 caracteres.")
            .When(x => x.ChaveIdempotencia is not null);
    }
}
