using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Api.Testes.Contas;

public class ContasIntegracaoTestes(AplicacaoFactory fabrica) : IClassFixture<AplicacaoFactory>
{
    private readonly HttpClient _cliente = fabrica.CriarClienteAutenticado();

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
        // Act
        var resposta = await _cliente.PostAsJsonAsync("/contas", new { clienteId = Guid.NewGuid() });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("id").GetGuid().Should().NotBeEmpty();
        corpo.GetProperty("saldoAtual").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task PostMovimentacoes_Credito_DeveRetornar201()
    {
        // Arrange
        var contaId = await CriarContaAsync();

        // Act
        var resposta = await _cliente.PostAsJsonAsync(
            $"/contas/{contaId}/movimentacoes",
            new { tipo = "Credito", valor = 100.00m, descricao = "Depósito" });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("valor").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task PostMovimentacoes_DebitoComSaldoSuficiente_DeveRetornar201()
    {
        // Arrange
        var contaId = await CriarContaAsync();
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Credito", valor = 200m });

        // Act
        var resposta = await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Debito", valor = 80m });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostMovimentacoes_DebitoSemSaldo_DeveRetornar422()
    {
        // Arrange
        var contaId = await CriarContaAsync();

        // Act
        var resposta = await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Debito", valor = 100m });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("title").GetString().Should().Be("Saldo insuficiente para realizar o débito");
    }

    [Fact]
    public async Task GetSaldo_DeveRetornarSaldoAtualCorreto()
    {
        // Arrange
        var contaId = await CriarContaAsync();
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 300m });
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Debito", valor = 50m });

        // Act
        var resposta = await _cliente.GetAsync($"/contas/{contaId}/saldo");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("saldo").GetDecimal().Should().Be(250m);
    }

    [Fact]
    public async Task GetSaldoEm_DeveRetornarSaldoHistoricoCorreto()
    {
        // Arrange
        var contaId = await CriarContaAsync();
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 100m });
        await Task.Delay(50);
        var marcaTemporal = DateTime.UtcNow;
        await Task.Delay(50);
        await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 200m });

        // Act
        var dataFormatada = Uri.EscapeDataString(marcaTemporal.ToString("o"));
        var resposta = await _cliente.GetAsync($"/contas/{contaId}/saldo?em={dataFormatada}");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("saldo").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task PostMovimentacoes_ComValorZero_DeveRetornar400()
    {
        // Arrange
        var contaId = await CriarContaAsync();

        // Act
        var resposta = await _cliente.PostAsJsonAsync($"/contas/{contaId}/movimentacoes",
            new { tipo = "Credito", valor = 0m });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSaldo_ContaInexistente_DeveRetornar404()
    {
        // Act
        var resposta = await _cliente.GetAsync($"/contas/{Guid.NewGuid()}/saldo");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostMovimentacoes_ComMesmaChaveIdempotencia_NaoDeveDuplicarLancamento()
    {
        // Arrange
        var clienteLocal = fabrica.CriarClienteAutenticado();
        var contaId = await CriarContaAsync();
        var chave = Guid.NewGuid().ToString();
        clienteLocal.DefaultRequestHeaders.Add("Idempotency-Key", chave);

        // Act
        await clienteLocal.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 100m });
        await clienteLocal.PostAsJsonAsync($"/contas/{contaId}/movimentacoes", new { tipo = "Credito", valor = 100m });

        // Assert
        var resposta = await _cliente.GetAsync($"/contas/{contaId}/saldo");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        corpo.GetProperty("saldo").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task GetSaude_DeveRetornar200()
    {
        // Act
        var resposta = await _cliente.GetAsync("/saude");

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequisicaoSemApiKey_Retorna401()
    {
        // Arrange
        var clienteSemChave = fabrica.CreateClient();

        // Act
        var resposta = await clienteSemChave.PostAsJsonAsync("/contas", new { clienteId = Guid.NewGuid() });

        // Assert
        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
