using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Api.Testes.Contas;

public class ContasIntegracaoTestes(FabricaDeAplicacao fabrica) : IClassFixture<FabricaDeAplicacao>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();
    private static readonly JsonSerializerOptions OpcoesJson = new() { PropertyNameCaseInsensitive = true };

    private async Task<Guid> CriarContaAsync()
    {
        var resposta = await _cliente.PostAsJsonAsync("/contas", new { clienteId = Guid.NewGuid() });
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task PostContas_DeveRetornar201ComIdGerado()
    {
        var resposta = await _cliente.PostAsJsonAsync("/contas", new { clienteId = Guid.NewGuid() });
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("saldoAtual").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task PostMovimentacoes_Credito_DeveRetornar201()
    {
        var contaId = await CriarContaAsync();
        var resposta = await _cliente.PostAsJsonAsync(
            $"/contas/{contaId}/movimentacoes",
            new { tipo = "Credito", valor = 100.00m, descricao = "Depósito" });
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("valor").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task PostMovimentacoes_DebitoComSaldoSuficiente_DeveRetornar201()
    {
        var contaId = await CriarContaAsync();
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Credito", valor = 200m });
        var resposta = await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Debito", valor = 80m });
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostMovimentacoes_DebitoSemSaldo_DeveRetornar422()
    {
        var contaId = await CriarContaAsync();
        var resposta = await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Debito", valor = 100m });
        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("tipo").GetString().Should().Be("saldo-insuficiente");
    }

    [Fact]
    public async Task GetSaldo_DeveRetornarSaldoAtualCorreto()
    {
        var contaId = await CriarContaAsync();
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 300m });
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Debito", valor = 50m });

        var resposta = await _cliente.GetAsync($"/contas/{contaId}/saldo");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("saldo").GetDecimal().Should().Be(250m);
    }

    [Fact]
    public async Task GetSaldoEm_DeveRetornarSaldoHistoricoCorreto()
    {
        var contaId = await CriarContaAsync();
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 100m });
        await Task.Delay(50);
        var marcaTemporal = DateTime.UtcNow;
        await Task.Delay(50);
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 200m });

        var dataFormatada = Uri.EscapeDataString(marcaTemporal.ToString("o"));
        var resposta = await _cliente.GetAsync($"/contas/{contaId}/saldo?em={dataFormatada}");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("saldo").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task PostMovimentacoes_ComValorZero_DeveRetornar400()
    {
        var contaId = await CriarContaAsync();
        var resposta = await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Credito", valor = 0m });
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSaldo_ContaInexistente_DeveRetornar404()
    {
        var resposta = await _cliente.GetAsync($"/contas/{Guid.NewGuid()}/saldo");
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostMovimentacoes_ComMesmaChaveIdempotencia_NaoDeveDuplicarLancamento()
    {
        var clienteLocal = fabrica.CreateClient();
        var contaId = await CriarContaAsync();
        var chave = Guid.NewGuid().ToString();
        clienteLocal.DefaultRequestHeaders.Add("Idempotency-Key", chave);

        await clienteLocal.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 100m });
        await clienteLocal.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 100m });

        var resposta = await _cliente.GetAsync($"/contas/{contaId}/saldo");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("saldo").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task GetSaude_DeveRetornar200()
    {
        var resposta = await _cliente.GetAsync("/saude");
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
