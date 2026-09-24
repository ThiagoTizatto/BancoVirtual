using MovimentacoesFinanceiras.Api;
using MovimentacoesFinanceiras.Aplicacao;
using MovimentacoesFinanceiras.Dominio;
using MovimentacoesFinanceiras.Infraestrutura;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) =>
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Destructure.With<MovimentacoesFinanceiras.Api.Logging.RedacaoDadosSensiveis>()
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

builder.Services
    .AddDominio()
    .AddAplicacao()
    .AddInfraestrutura(builder.Configuration)
    .AddApi(builder.Configuration);

if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddRabbitMqConsumer();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.AplicarMigracoesAsync();

    var topologia = app.Services.GetRequiredService<MovimentacoesFinanceiras.Infraestrutura.RabbitMq.TopologiaDeclarator>();
    await topologia.DeclararAsync();
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI(opcoes =>
{
    opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Movimentações Financeiras v1");
    opcoes.RoutePrefix = string.Empty;
});

app.UseSerilogRequestLogging();
app.UseMiddleware<MovimentacoesFinanceiras.Api.Middlewares.CorrelacaoIdMiddleware>();
app.UseMiddleware<MovimentacoesFinanceiras.Api.Middlewares.TratadorDeExcecoesMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

await app.RunAsync();

public partial class Program { }
