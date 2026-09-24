using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Carga.Testes;

public static class CenarioMovimentacoes
{
    public static ScenarioProps Criar(string baseUrl, string apiKey, Guid contaId)
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        return Scenario.Create("creditos_concorrentes", async _ =>
        {
            var requisicao = Http.CreateRequest("POST", $"{baseUrl}/contas/{contaId}/movimentacoes")
                .WithHeader("X-Api-Key", apiKey)
                .WithJsonBody(new { tipo = "Credito", valor = 10.0m, descricao = "carga" });

            return await Http.Send(http, requisicao);
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)));
    }
}
