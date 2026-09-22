using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MovimentacoesFinanceiras.Aplicacao.Contas.Comportamentos;
using MovimentacoesFinanceiras.Aplicacao.Contas.Comandos.RegistrarMovimentacao;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia;
using MovimentacoesFinanceiras.Infraestrutura.Persistencia.Repositorios;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) =>
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

builder.Services.AddDbContext<ContextoBancoDados>(opcoes =>
    opcoes.UseSqlite(builder.Configuration.GetConnectionString("Padrao")
        ?? "Data Source=movimentacoes.db"));

builder.Services.AddScoped<IContaRepositorio, ContaRepositorio>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(RegistrarMovimentacaoComando).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(RegistrarMovimentacaoComando).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidacaoBehavior<,>));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new()
    {
        Title = "API de Movimentações Financeiras",
        Description = "Registra movimentações financeiras e consulta saldos de contas bancárias. " +
                      "Utiliza CQRS com ledger append-only para rastreabilidade completa.",
        Version = "v1"
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ContextoBancoDados>("banco-de-dados");

var app = builder.Build();

using (var escopo = app.Services.CreateScope())
{
    var contexto = escopo.ServiceProvider.GetRequiredService<ContextoBancoDados>();
    await contexto.Database.EnsureCreatedAsync();
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

app.Run();

public partial class Program { }
