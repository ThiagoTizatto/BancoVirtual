using System.Text.Json;
using FluentValidation;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;

namespace MovimentacoesFinanceiras.Api.Middlewares;

public class TratadorDeExcecoesMiddleware(RequestDelegate proximo, ILogger<TratadorDeExcecoesMiddleware> logger)
{
    private static readonly JsonSerializerOptions OpcoesJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await proximo(contexto);
        }
        catch (ValidationException ex)
        {
            contexto.Response.StatusCode = StatusCodes.Status400BadRequest;
            contexto.Response.ContentType = "application/problem+json";
            var problema = new
            {
                tipo = "requisicao-invalida",
                titulo = "A requisição contém dados inválidos",
                status = 400,
                erros = ex.Errors.Select(e => e.ErrorMessage)
            };
            await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, OpcoesJson));
        }
        catch (SaldoInsuficienteException ex)
        {
            contexto.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            contexto.Response.ContentType = "application/problem+json";
            var problema = new
            {
                tipo = "saldo-insuficiente",
                titulo = "Saldo insuficiente para realizar o débito",
                status = 422,
                detalhe = ex.Message
            };
            await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, OpcoesJson));
        }
        catch (ContaNaoEncontradaException ex)
        {
            contexto.Response.StatusCode = StatusCodes.Status404NotFound;
            contexto.Response.ContentType = "application/problem+json";
            var problema = new
            {
                tipo = "conta-nao-encontrada",
                titulo = "Conta não encontrada",
                status = 404,
                detalhe = ex.Message
            };
            await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, OpcoesJson));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro nao tratado");
            contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
            contexto.Response.ContentType = "application/problem+json";
            var problema = new
            {
                tipo = "erro-interno",
                titulo = "Ocorreu um erro interno. Tente novamente.",
                status = 500
            };
            await contexto.Response.WriteAsync(JsonSerializer.Serialize(problema, OpcoesJson));
        }
    }
}
