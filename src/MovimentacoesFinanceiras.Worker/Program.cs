using MovimentacoesFinanceiras.Aplicacao;
using MovimentacoesFinanceiras.Infraestrutura;
using MovimentacoesFinanceiras.Worker;
using MovimentacoesFinanceiras.Worker.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAplicacao();
builder.Services.AddInfraestrutura(builder.Configuration);
builder.Services.AddWorker(builder.Configuration);

var app = builder.Build();

await app.Services.AplicarMigracoesAsync();

var topologia = app.Services.GetRequiredService<TopologiaDeclarator>();
await topologia.DeclararAsync();

await app.RunAsync();
