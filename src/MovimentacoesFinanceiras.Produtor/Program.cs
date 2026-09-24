using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Produtor;
using MovimentacoesFinanceiras.Produtor.RabbitMq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProdutor(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes => opcoes.SwaggerDoc("v1", new()
{
    Title = "Produtor de Movimentações Financeiras",
    Description = "Publica comandos de crédito/débito na fila RabbitMQ. " +
                  "A persistência é assíncrona — o endpoint retorna 202 Accepted imediatamente.",
    Version = "v1"
}));

var app = builder.Build();

var topologia = app.Services.GetRequiredService<MovimentacoesFinanceiras.Produtor.RabbitMq.TopologiaDeclaratorProdutor>();
await topologia.DeclararAsync();

app.UseSwagger();
app.UseSwaggerUI(opcoes =>
{
    opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "Produtor de Movimentações Financeiras v1");
    opcoes.RoutePrefix = string.Empty;
});

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
})
.WithName("PublicarMovimentacao")
.WithSummary("Publica uma movimentação na fila RabbitMQ")
.WithDescription("Enfileira um comando de crédito ou débito. Retorna 202 Accepted imediatamente; a Api processa e persiste de forma assíncrona.")
.WithTags("Movimentacoes")
.Produces(StatusCodes.Status202Accepted);

await app.RunAsync();
