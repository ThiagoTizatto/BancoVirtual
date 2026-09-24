using System.Net.Http.Json;
using Carga.Testes;
using NBomber.CSharp;

var baseUrl = Environment.GetEnvironmentVariable("CARGA_BASE_URL") ?? "http://localhost:8080";
var apiKey = Environment.GetEnvironmentVariable("CARGA_API_KEY") ?? "dev-key-local-somente";

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
var criacao = await http.PostAsJsonAsync($"{baseUrl}/contas", new { clienteId = Guid.NewGuid() });
criacao.EnsureSuccessStatusCode();
var conta = await criacao.Content.ReadFromJsonAsync<ContaCriada>();

NBomberRunner
    .RegisterScenarios(CenarioMovimentacoes.Criar(baseUrl, apiKey, conta!.Id))
    .Run();

record ContaCriada(Guid Id);
