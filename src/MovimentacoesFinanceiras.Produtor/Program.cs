using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Produtor;
using MovimentacoesFinanceiras.Produtor.RabbitMq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProdutor(builder.Configuration);

var app = builder.Build();

app.MapPost("/movimentacoes", async (
    MovimentacaoRequest request,
    IPublicadorDeMovimentacoes publicador,
    CancellationToken ct) =>
{
    var mensagem = new ComandoMovimentacaoMessage(
        request.ContaId,
        request.Tipo,
        request.Valor,
        request.Descricao,
        request.ChaveIdempotencia);

    await publicador.PublicarAsync(mensagem, ct);

    return Results.Accepted();
});

await app.RunAsync();
