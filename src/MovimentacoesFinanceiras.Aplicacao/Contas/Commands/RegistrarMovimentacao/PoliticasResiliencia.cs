using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;

public static class PoliticasResiliencia
{
    // Retry para conflitos de concorrência — falha transitória, esperada sob carga.
    private static readonly AsyncRetryPolicy Retry = Policy
        .Handle<DbUpdateConcurrencyException>()
        .WaitAndRetryAsync(3, tentativa => TimeSpan.FromMilliseconds(50 * Math.Pow(2, tentativa)));

    // Circuit breaker para indisponibilidade do banco — abre após falhas de
    // conectividade consecutivas, falhando rápido para dar tempo de recuperação.
    private static readonly AsyncCircuitBreakerPolicy Breaker = Policy
        .Handle<NpgsqlException>()
        .Or<DbUpdateException>(ex => ex is not DbUpdateConcurrencyException)
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(15));

    // Breaker (externo) envolve o Retry (interno): concorrência é retentada;
    // falha de conectividade abre o circuito sem retentar contra um banco morto.
    public static readonly IAsyncPolicy Combinada = Policy.WrapAsync(Breaker, Retry);
}
