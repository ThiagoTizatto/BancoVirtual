using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;

namespace Api.Testes.Contas;

public class ContasConcorrenciaTestes(AplicacaoFactory fabrica) : IClassFixture<AplicacaoFactory>
{
    private static async Task<Guid> CriarContaComSaldo(HttpClient cliente, decimal saldoInicial)
    {
        var resposta = await cliente.PostAsJsonAsync("/contas", new { clienteId = Guid.NewGuid() });
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var contaId = json.GetProperty("id").GetGuid();

        await cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Credito", valor = saldoInicial });

        return contaId;
    }

    [Fact]
    public async Task DebitosSimultaneos_NaoDevemGerarSaldoNegativo()
    {
        // Arrange
        var cliente = fabrica.CriarClienteAutenticado();
        var contaId = await CriarContaComSaldo(cliente, 100m);

        // Act
        var tarefas = Enumerable.Range(0, 10)
            .Select(_ => cliente.PostAsJsonAsync(
                $"/contas/{contaId}/movimentacoes",
                new { tipo = "Debito", valor = 20m }))
            .ToList();

        var respostas = await Task.WhenAll(tarefas);

        // Assert
        var sucessos = respostas.Count(r => r.StatusCode == HttpStatusCode.Created);
        var falhas422 = respostas.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);

        // Exatamente 5 débitos de R$20 cabem num saldo de R$100
        sucessos.Should().Be(5, "apenas 5 débitos de R$20 cabem num saldo de R$100");
        falhas422.Should().BeGreaterThan(0, "os demais devem falhar por saldo insuficiente");

        var respostaSaldo = await cliente.GetAsync($"/contas/{contaId}/saldo");
        var json = await respostaSaldo.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("saldo").GetDecimal().Should().Be(0m, "o saldo deve ser exatamente zero");
    }

    [Fact]
    public async Task CreditosSimultaneos_DevemSerTodosPersistidos()
    {
        // Arrange
        var cliente = fabrica.CriarClienteAutenticado();
        var contaId = await CriarContaComSaldo(cliente, 0.01m);

        // Act
        var tarefas = Enumerable.Range(0, 10)
            .Select(_ => cliente.PostAsJsonAsync(
                $"/contas/{contaId}/movimentacoes",
                new { tipo = "Credito", valor = 50m }))
            .ToList();

        var respostas = await Task.WhenAll(tarefas);

        // Assert
        // Com update atômico no banco, créditos são incrementos incondicionais:
        // NENHUM se perde sob concorrência, todos retornam 201.
        respostas.All(r => r.StatusCode == HttpStatusCode.Created)
            .Should().BeTrue("todos os créditos devem ser aceitos e persistidos");

        var respostaSaldo = await cliente.GetAsync($"/contas/{contaId}/saldo");
        var json = await respostaSaldo.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("saldo").GetDecimal().Should().Be(500.01m,
            "0.01 inicial + 10 créditos de R$50 = R$500,01");
    }

    [Fact]
    public async Task AposMultiplasOperacoes_SaldoSnapshotDeveSerIgualAoSomaDosLancamentos()
    {
        // Arrange
        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<BancoDadosContext>();
        var cliente = fabrica.CriarClienteAutenticado();
        var contaId = await CriarContaComSaldo(cliente, 0.01m);

        // Act
        var creditosTarefas = Enumerable.Range(0, 20)
            .Select(_ => cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
                new { tipo = "Credito", valor = 50m }));
        await Task.WhenAll(creditosTarefas);

        var debitosTarefas = Enumerable.Range(0, 5)
            .Select(_ => cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
                new { tipo = "Debito", valor = 30m }));
        await Task.WhenAll(debitosTarefas);

        // Assert
        var conta = await contexto.Contas.FindAsync(contaId);
        var saldoSnapshot = conta!.SaldoAtual;

        var lancamentos = contexto.Lancamentos.Where(l => l.ContaId == contaId).ToList();
        var saldoLedger = lancamentos.Sum(l => l.Tipo == TipoLancamento.Credito ? l.Valor : -l.Valor);

        saldoSnapshot.Quantia.Should().Be(saldoLedger,
            "o snapshot saldo_atual nunca deve divergir do somatório dos lançamentos");
    }
}
