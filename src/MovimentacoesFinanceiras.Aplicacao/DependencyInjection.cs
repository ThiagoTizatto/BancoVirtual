using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Aplicacao.Contas.Behaviors;
using MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;

namespace MovimentacoesFinanceiras.Aplicacao;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(RegistrarMovimentacaoCommand).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddSingleton<MovimentacoesFinanceiras.Aplicacao.Metricas.MetricasMovimentacao>();

        return services;
    }
}
