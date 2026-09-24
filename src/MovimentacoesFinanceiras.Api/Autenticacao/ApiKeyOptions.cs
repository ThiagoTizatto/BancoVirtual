using Microsoft.AspNetCore.Authentication;

namespace MovimentacoesFinanceiras.Api.Autenticacao;

public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    public const string Esquema = "ApiKey";
    public const string NomeCabecalho = "X-Api-Key";

    public IReadOnlyCollection<string> ChavesValidas { get; set; } = [];
}
