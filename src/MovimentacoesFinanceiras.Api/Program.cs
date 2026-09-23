using Microsoft.EntityFrameworkCore;
using MovimentacoesFinanceiras.Api;
using MovimentacoesFinanceiras.Aplicacao;
using MovimentacoesFinanceiras.Dominio;
using MovimentacoesFinanceiras.Infraestrutura;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) =>
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

builder.Services
    .AddDominio()
    .AddAplicacao()
    .AddInfraestrutura(builder.Configuration)
    .AddApi();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var escopo = app.Services.CreateScope();
    var contexto = escopo.ServiceProvider.GetRequiredService<BancoDadosContext>();
    await contexto.Database.MigrateAsync();
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
app.MapControllers();
app.MapHealthChecks("/saude");

await app.RunAsync();

public partial class Program { }
