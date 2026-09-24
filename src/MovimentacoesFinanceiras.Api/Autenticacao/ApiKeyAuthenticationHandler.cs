using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MovimentacoesFinanceiras.Api.Autenticacao;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyOptions.NomeCabecalho, out var valores))
            return Task.FromResult(AuthenticateResult.NoResult());

        var chaveRecebida = valores.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(chaveRecebida) || !Options.ChavesValidas.Contains(chaveRecebida))
            return Task.FromResult(AuthenticateResult.Fail("API Key inválida."));

        var identidade = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "servico-autenticado")],
            ApiKeyOptions.Esquema);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identidade), ApiKeyOptions.Esquema);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
