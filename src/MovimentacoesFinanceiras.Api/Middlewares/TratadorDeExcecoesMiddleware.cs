using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace MovimentacoesFinanceiras.Api.Middlewares;

public class TratadorDeExcecoesMiddleware(
    RequestDelegate proximo,
    IProblemDetailsService problemDetailsService,
    ILogger<TratadorDeExcecoesMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        try
        {
            await proximo(contexto);
        }
        catch (ValidationException ex)
        {
            await EscreverProblemaAsync(contexto, StatusCodes.Status400BadRequest,
                "A requisição contém dados inválidos", null,
                new Dictionary<string, object?> { ["erros"] = ex.Errors.Select(e => e.ErrorMessage).ToArray() });
        }
        catch (SaldoInsuficienteException ex)
        {
            await EscreverProblemaAsync(contexto, StatusCodes.Status422UnprocessableEntity,
                "Saldo insuficiente para realizar o débito", ex.Message, null);
        }
        catch (ContaNaoEncontradaException ex)
        {
            await EscreverProblemaAsync(contexto, StatusCodes.Status404NotFound,
                "Conta não encontrada", ex.Message, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro nao tratado");
            await EscreverProblemaAsync(contexto, StatusCodes.Status500InternalServerError,
                "Ocorreu um erro interno. Tente novamente.", null, null);
        }
    }

    private async Task EscreverProblemaAsync(
        HttpContext contexto, int status, string titulo, string? detalhe,
        IDictionary<string, object?>? extensoes)
    {
        contexto.Response.StatusCode = status;

        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe,
            Instance = contexto.Request.Path
        };

        if (extensoes is not null)
        {
            foreach (var (chave, valor) in extensoes)
                problema.Extensions[chave] = valor;
        }

        if (contexto.Items.TryGetValue("correlacao_id", out var correlacao) && correlacao is not null)
            problema.Extensions["correlacao_id"] = correlacao;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema
        });
    }
}
