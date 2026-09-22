using Serilog.Context;

namespace MovimentacoesFinanceiras.Api.Middlewares;

public class CorrelacaoIdMiddleware(RequestDelegate proximo)
{
    private const string CabecalhoCorrelacaoId = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext contexto)
    {
        var correlacaoId = contexto.Request.Headers[CabecalhoCorrelacaoId].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        contexto.Response.Headers[CabecalhoCorrelacaoId] = correlacaoId;

        using (LogContext.PushProperty("correlacao_id", correlacaoId))
        {
            await proximo(contexto);
        }
    }
}
