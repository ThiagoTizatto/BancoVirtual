using FluentValidation;
using MediatR;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validadores)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!validadores.Any())
            return await next(cancellationToken);

        var contexto = new ValidationContext<TRequest>(request);
        var erros = validadores
            .Select(v => v.Validate(contexto))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (erros.Count > 0)
            throw new ValidationException(erros);

        return await next(cancellationToken);
    }
}
