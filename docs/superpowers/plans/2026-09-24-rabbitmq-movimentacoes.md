# RabbitMQ — Movimentações via Fila: Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adicionar canal assíncrono via RabbitMQ para registrar créditos e débitos, mantendo o endpoint HTTP síncrono existente (dual path).

**Architecture:** `MovimentacoesFinanceiras.Mensagens` (classlib) carrega o contrato da mensagem e as constantes de topologia. `MovimentacoesFinanceiras.Worker` (Worker Service) consome a fila e despacha o `RegistrarMovimentacaoCommand` existente via MediatR — reutilizando todo o pipeline (validação, idempotência, Polly, métricas). `MovimentacoesFinanceiras.Produtor` (Minimal API) recebe `POST /movimentacoes` e publica na fila, sem conhecimento de domínio além do enum `TipoLancamento`. A resiliência de reconexão com o broker é fornecida por `AutomaticRecoveryEnabled = true` (padrão do RabbitMQ.Client v7). A resiliência de banco já existe em `PoliticasResiliencia.Combinada` — o Worker simplesmente ack/nack.

**Tech Stack:** .NET 10, RabbitMQ.Client 7.x, MediatR 12.x, Polly 8.x (existente), Testcontainers 4.x, NSubstitute 5.x, xUnit 2.x.

---

## Mapa de Arquivos

### Novos

```
Directory.Build.props                                     ← adicionar RabbitMqClientVersion, NSubstituteVersion
MovimentacoesFinanceiras.slnx                             ← adicionar 4 novos projetos

src/MovimentacoesFinanceiras.Mensagens/
  MovimentacoesFinanceiras.Mensagens.csproj
  ComandoMovimentacaoMessage.cs
  TopologiaRabbitMq.cs

src/MovimentacoesFinanceiras.Worker/
  MovimentacoesFinanceiras.Worker.csproj
  Program.cs
  DependencyInjection.cs
  appsettings.json
  Dockerfile
  Mensageria/
    RabbitMqOptions.cs
    IConexaoRabbitMq.cs
    ConexaoRabbitMq.cs
    TopologiaDeclarator.cs
    ConsumidorDeMovimentacoes.cs
  Processamento/
    ProcessadorDeMovimentacao.cs

src/MovimentacoesFinanceiras.Produtor/
  MovimentacoesFinanceiras.Produtor.csproj
  Program.cs
  DependencyInjection.cs
  appsettings.json
  Dockerfile
  Mensageria/
    RabbitMqOptions.cs
    IConexaoRabbitMq.cs
    ConexaoRabbitMq.cs
    IPublicadorDeMovimentacoes.cs
    PublicadorRabbitMq.cs

tests/Worker.Testes/
  Worker.Testes.csproj
  Processamento/
    ProcessadorDeMovimentacaoTestes.cs

tests/Produtor.Testes/
  Produtor.Testes.csproj
  Mensageria/
    PublicadorRabbitMqTestes.cs
```

### Modificados

```
docker-compose.yml   ← adicionar rabbitmq, worker, produtor
```

---

## Task 1: Projeto Mensagens + versões NuGet + slnx

**Files:**
- Modify: `Directory.Build.props`
- Create: `src/MovimentacoesFinanceiras.Mensagens/MovimentacoesFinanceiras.Mensagens.csproj`
- Create: `src/MovimentacoesFinanceiras.Mensagens/ComandoMovimentacaoMessage.cs`
- Create: `src/MovimentacoesFinanceiras.Mensagens/TopologiaRabbitMq.cs`
- Modify: `MovimentacoesFinanceiras.slnx`

- [ ] **Step 1: Adicionar versões ao `Directory.Build.props`**

Abra `Directory.Build.props` e adicione as duas linhas ao bloco `<!-- Aplicação -->`:

```xml
<RabbitMqClientVersion>7.*</RabbitMqClientVersion>
<NSubstituteVersion>5.*</NSubstituteVersion>
```

Resultado do bloco após a edição:

```xml
<!-- Aplicação -->
<FluentValidationVersion>11.*</FluentValidationVersion>
<MediatRVersion>12.*</MediatRVersion>
<NpgsqlVersion>9.*</NpgsqlVersion>
<PollyVersion>8.*</PollyVersion>
<RabbitMqClientVersion>7.*</RabbitMqClientVersion>
<NSubstituteVersion>5.*</NSubstituteVersion>
```

- [ ] **Step 2: Criar `MovimentacoesFinanceiras.Mensagens.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <ItemGroup>
        <ProjectReference Include="..\MovimentacoesFinanceiras.Dominio\MovimentacoesFinanceiras.Dominio.csproj" />
    </ItemGroup>

</Project>
```

> `Mensagens` referencia `Dominio` para reutilizar `TipoLancamento` sem duplicação.

- [ ] **Step 3: Criar `ComandoMovimentacaoMessage.cs`**

```csharp
using MovimentacoesFinanceiras.Dominio.Contas;

namespace MovimentacoesFinanceiras.Mensagens;

public record ComandoMovimentacaoMessage(
    Guid ContaId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao,
    string? ChaveIdempotencia
);
```

- [ ] **Step 4: Criar `TopologiaRabbitMq.cs`**

```csharp
namespace MovimentacoesFinanceiras.Mensagens;

public static class TopologiaRabbitMq
{
    public const string Exchange      = "movimentacoes";
    public const string ExchangeDlx   = "movimentacoes.dlx";
    public const string FilaPrincipal = "movimentacoes.registrar";
    public const string FilaDlq       = "movimentacoes.registrar.dlq";
    public const string RoutingKey    = "registrar";
    public const string RoutingKeyDlq = "registrar.dlq";
}
```

- [ ] **Step 5: Atualizar `MovimentacoesFinanceiras.slnx`**

Adicione as entradas dos novos projetos:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/MovimentacoesFinanceiras.Api/MovimentacoesFinanceiras.Api.csproj" />
    <Project Path="src/MovimentacoesFinanceiras.Aplicacao/MovimentacoesFinanceiras.Aplicacao.csproj" />
    <Project Path="src/MovimentacoesFinanceiras.Dominio/MovimentacoesFinanceiras.Dominio.csproj" />
    <Project Path="src/MovimentacoesFinanceiras.Infraestrutura/MovimentacoesFinanceiras.Infraestrutura.csproj" />
    <Project Path="src/MovimentacoesFinanceiras.Mensagens/MovimentacoesFinanceiras.Mensagens.csproj" />
    <Project Path="src/MovimentacoesFinanceiras.Worker/MovimentacoesFinanceiras.Worker.csproj" />
    <Project Path="src/MovimentacoesFinanceiras.Produtor/MovimentacoesFinanceiras.Produtor.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Api.Testes/Api.Testes.csproj" />
    <Project Path="tests/Dominio.Testes/Dominio.Testes.csproj" />
    <Project Path="tests/Worker.Testes/Worker.Testes.csproj" />
    <Project Path="tests/Produtor.Testes/Produtor.Testes.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 6: Verificar build**

```bash
dotnet build src/MovimentacoesFinanceiras.Mensagens/MovimentacoesFinanceiras.Mensagens.csproj
```

Esperado: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add Directory.Build.props MovimentacoesFinanceiras.slnx \
        src/MovimentacoesFinanceiras.Mensagens/
git commit -m "feat: add Mensagens classlib with message contract and topology constants"
```

---

## Task 2: Worker — `ProcessadorDeMovimentacao` (TDD)

**Files:**
- Create: `tests/Worker.Testes/Worker.Testes.csproj`
- Create: `tests/Worker.Testes/Processamento/ProcessadorDeMovimentacaoTestes.cs`
- Create: `src/MovimentacoesFinanceiras.Worker/MovimentacoesFinanceiras.Worker.csproj`
- Create: `src/MovimentacoesFinanceiras.Worker/Processamento/ProcessadorDeMovimentacao.cs`

- [ ] **Step 1: Criar `Worker.Testes.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="coverlet.collector" Version="$(CoverletVersion)" />
        <PackageReference Include="FluentAssertions" Version="$(FluentAssertionsVersion)" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="$(TestSdkVersion)" />
        <PackageReference Include="NSubstitute" Version="$(NSubstituteVersion)" />
        <PackageReference Include="xunit" Version="$(XunitVersion)" />
        <PackageReference Include="xunit.runner.visualstudio" Version="$(XunitRunnerVersion)" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="Xunit" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\MovimentacoesFinanceiras.Worker\MovimentacoesFinanceiras.Worker.csproj" />
    </ItemGroup>

</Project>
```

- [ ] **Step 2: Criar `ProcessadorDeMovimentacaoTestes.cs`**

```csharp
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Dominio.Contas.Excecoes;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Worker.Processamento;
using NSubstitute;

namespace Worker.Testes.Processamento;

public class ProcessadorDeMovimentacaoTestes
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private ProcessadorDeMovimentacao CriarProcessador() =>
        new(_mediator, NullLogger<ProcessadorDeMovimentacao>.Instance);

    [Fact]
    public async Task ProcessarAsync_MensagemValida_EnviaCommandCorretoAoMediador()
    {
        // Arrange
        var processador = CriarProcessador();
        var mensagem = new ComandoMovimentacaoMessage(
            ContaId: Guid.NewGuid(),
            Tipo: TipoLancamento.Credito,
            Valor: 150m,
            Descricao: "Depósito",
            ChaveIdempotencia: Guid.NewGuid().ToString());

        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem);

        // Act
        await processador.ProcessarAsync(corpo, CancellationToken.None);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<RegistrarMovimentacaoCommand>(c =>
                c.ContaId == mensagem.ContaId &&
                c.Tipo == TipoLancamento.Credito &&
                c.Valor == 150m &&
                c.ChaveIdempotencia == mensagem.ChaveIdempotencia),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessarAsync_ErroNegocio_PropagaExcecaoSemRetry()
    {
        // Arrange
        var processador = CriarProcessador();
        _mediator
            .Send(Arg.Any<RegistrarMovimentacaoCommand>(), Arg.Any<CancellationToken>())
            .Returns<LancamentoResponse>(_ => throw new SaldoInsuficienteException(0m, Dinheiro.De(100m)));

        var mensagem = new ComandoMovimentacaoMessage(
            Guid.NewGuid(), TipoLancamento.Debito, 100m, null, null);
        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem);

        // Act
        var acao = () => processador.ProcessarAsync(corpo, CancellationToken.None);

        // Assert
        await acao.Should().ThrowAsync<SaldoInsuficienteException>();
        await _mediator.Received(1).Send(
            Arg.Any<RegistrarMovimentacaoCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessarAsync_CorpoInvalido_LancaInvalidOperationException()
    {
        // Arrange
        var processador = CriarProcessador();
        var corpoInvalido = "nao-e-json"u8.ToArray();

        // Act
        var acao = () => processador.ProcessarAsync(corpoInvalido, CancellationToken.None);

        // Assert
        await acao.Should().ThrowAsync<Exception>();
    }
}
```

- [ ] **Step 3: Criar `MovimentacoesFinanceiras.Worker.csproj`** (mínimo para compilar o teste)

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">

    <ItemGroup>
        <PackageReference Include="RabbitMQ.Client" Version="$(RabbitMqClientVersion)" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\MovimentacoesFinanceiras.Mensagens\MovimentacoesFinanceiras.Mensagens.csproj" />
        <ProjectReference Include="..\MovimentacoesFinanceiras.Aplicacao\MovimentacoesFinanceiras.Aplicacao.csproj" />
        <ProjectReference Include="..\MovimentacoesFinanceiras.Infraestrutura\MovimentacoesFinanceiras.Infraestrutura.csproj" />
    </ItemGroup>

</Project>
```

- [ ] **Step 4: Rodar os testes para confirmar que falham**

```bash
dotnet test tests/Worker.Testes/Worker.Testes.csproj
```

Esperado: erros de compilação — `ProcessadorDeMovimentacao` não existe ainda.

- [ ] **Step 5: Criar `ProcessadorDeMovimentacao.cs`**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using MovimentacoesFinanceiras.Aplicacao.Contas.Commands.RegistrarMovimentacao;
using MovimentacoesFinanceiras.Mensagens;

namespace MovimentacoesFinanceiras.Worker.Processamento;

public sealed class ProcessadorDeMovimentacao(
    IMediator mediator,
    ILogger<ProcessadorDeMovimentacao> logger)
{
    public async Task ProcessarAsync(byte[] corpo, CancellationToken ct)
    {
        var mensagem = JsonSerializer.Deserialize<ComandoMovimentacaoMessage>(corpo)
            ?? throw new InvalidOperationException("Corpo da mensagem é nulo após deserialização.");

        logger.LogInformation(
            "Processando movimentação ContaId={ContaId} Tipo={Tipo} Valor={Valor}",
            mensagem.ContaId, mensagem.Tipo, mensagem.Valor);

        var command = new RegistrarMovimentacaoCommand(
            mensagem.ContaId,
            mensagem.Tipo,
            mensagem.Valor,
            mensagem.Descricao,
            mensagem.ChaveIdempotencia);

        await mediator.Send(command, ct);
    }
}
```

- [ ] **Step 6: Rodar os testes para confirmar que passam**

```bash
dotnet test tests/Worker.Testes/Worker.Testes.csproj --logger "console;verbosity=normal"
```

Esperado: `3 passed, 0 failed`.

- [ ] **Step 7: Commit**

```bash
git add src/MovimentacoesFinanceiras.Worker/ tests/Worker.Testes/
git commit -m "feat: add Worker project with ProcessadorDeMovimentacao (TDD)"
```

---

## Task 3: Worker — Infraestrutura RabbitMQ + DI + Program.cs

**Files:**
- Create: `src/MovimentacoesFinanceiras.Worker/Mensageria/RabbitMqOptions.cs`
- Create: `src/MovimentacoesFinanceiras.Worker/Mensageria/IConexaoRabbitMq.cs`
- Create: `src/MovimentacoesFinanceiras.Worker/Mensageria/ConexaoRabbitMq.cs`
- Create: `src/MovimentacoesFinanceiras.Worker/Mensageria/TopologiaDeclarator.cs`
- Create: `src/MovimentacoesFinanceiras.Worker/Mensageria/ConsumidorDeMovimentacoes.cs`
- Create: `src/MovimentacoesFinanceiras.Worker/DependencyInjection.cs`
- Create: `src/MovimentacoesFinanceiras.Worker/Program.cs`
- Create: `src/MovimentacoesFinanceiras.Worker/appsettings.json`

- [ ] **Step 1: Criar `RabbitMqOptions.cs`**

```csharp
namespace MovimentacoesFinanceiras.Worker.Mensageria;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
```

- [ ] **Step 2: Criar `IConexaoRabbitMq.cs`**

```csharp
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Worker.Mensageria;

public interface IConexaoRabbitMq
{
    Task<IChannel> CriarCanalAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3: Criar `ConexaoRabbitMq.cs`**

```csharp
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Worker.Mensageria;

public sealed class ConexaoRabbitMq : IConexaoRabbitMq, IDisposable
{
    private readonly IConnection _conexao;

    private ConexaoRabbitMq(IConnection conexao) => _conexao = conexao;

    public static async Task<ConexaoRabbitMq> CriarAsync(RabbitMqOptions opcoes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        var factory = new ConnectionFactory
        {
            HostName = opcoes.Host,
            Port = opcoes.Port,
            VirtualHost = opcoes.VirtualHost,
            UserName = opcoes.UserName,
            Password = opcoes.Password,
            AutomaticRecoveryEnabled = true
        };

        var conexao = await factory.CreateConnectionAsync(ct);
        return new ConexaoRabbitMq(conexao);
    }

    public async Task<IChannel> CriarCanalAsync(CancellationToken ct = default) =>
        await _conexao.CreateChannelAsync(cancellationToken: ct);

    public void Dispose() => _conexao.Dispose();
}
```

- [ ] **Step 4: Criar `TopologiaDeclarator.cs`**

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovimentacoesFinanceiras.Mensagens;
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Worker.Mensageria;

public sealed class TopologiaDeclarator(
    IConexaoRabbitMq conexao,
    ILogger<TopologiaDeclarator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var canal = await conexao.CriarCanalAsync(cancellationToken);

        await canal.ExchangeDeclareAsync(
            TopologiaRabbitMq.Exchange, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);

        await canal.ExchangeDeclareAsync(
            TopologiaRabbitMq.ExchangeDlx, ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);

        var argsPrincipal = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = TopologiaRabbitMq.ExchangeDlx,
            ["x-dead-letter-routing-key"] = TopologiaRabbitMq.RoutingKeyDlq
        };
        await canal.QueueDeclareAsync(
            TopologiaRabbitMq.FilaPrincipal,
            durable: true, exclusive: false, autoDelete: false,
            arguments: argsPrincipal, cancellationToken: cancellationToken);

        await canal.QueueBindAsync(
            TopologiaRabbitMq.FilaPrincipal, TopologiaRabbitMq.Exchange,
            TopologiaRabbitMq.RoutingKey, cancellationToken: cancellationToken);

        await canal.QueueDeclareAsync(
            TopologiaRabbitMq.FilaDlq,
            durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);

        await canal.QueueBindAsync(
            TopologiaRabbitMq.FilaDlq, TopologiaRabbitMq.ExchangeDlx,
            TopologiaRabbitMq.RoutingKeyDlq, cancellationToken: cancellationToken);

        logger.LogInformation("Topologia RabbitMQ declarada com sucesso.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 5: Criar `ConsumidorDeMovimentacoes.cs`**

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Worker.Processamento;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MovimentacoesFinanceiras.Worker.Mensageria;

public sealed class ConsumidorDeMovimentacoes(
    IConexaoRabbitMq conexao,
    ProcessadorDeMovimentacao processador,
    ILogger<ConsumidorDeMovimentacoes> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var canal = await conexao.CriarCanalAsync(stoppingToken);
        await canal.SetQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(canal);
        consumer.ReceivedAsync += async (_, ea) =>
            await ProcessarMensagemAsync(canal, ea, stoppingToken);

        await canal.BasicConsumeAsync(
            TopologiaRabbitMq.FilaPrincipal,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Consumidor iniciado. Aguardando mensagens em '{Fila}'.", TopologiaRabbitMq.FilaPrincipal);

        await Task.Delay(Timeout.Infinite, stoppingToken)
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private async Task ProcessarMensagemAsync(IChannel canal, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        try
        {
            await processador.ProcessarAsync(ea.Body.ToArray(), ct);
            await canal.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
            logger.LogInformation("Mensagem processada. DeliveryTag={Tag}", ea.DeliveryTag);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Falha ao processar mensagem. DeliveryTag={Tag}. Enviando para DLQ.",
                ea.DeliveryTag);
            await canal.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
        }
    }
}
```

> **Nota:** Erros de domínio (`SaldoInsuficienteException`, `ContaNaoEncontradaException`) e de infraestrutura são tratados da mesma forma pelo consumidor — nack + DLQ. A diferença está no pipeline interno: `PoliticasResiliencia.Combinada` (já existente no `RegistrarMovimentacaoHandler`) retenta erros de banco antes de propagar. Se Polly esgotar as tentativas, a exceção chega aqui e a mensagem vai para a DLQ.

- [ ] **Step 6: Criar `DependencyInjection.cs`**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Worker.Mensageria;
using MovimentacoesFinanceiras.Worker.Processamento;

namespace MovimentacoesFinanceiras.Worker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var opcoes = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddSingleton<IConexaoRabbitMq>(_ =>
            ConexaoRabbitMq.CriarAsync(opcoes).GetAwaiter().GetResult());

        services.AddHostedService<TopologiaDeclarator>();
        services.AddSingleton<ProcessadorDeMovimentacao>();
        services.AddHostedService<ConsumidorDeMovimentacoes>();

        return services;
    }
}
```

- [ ] **Step 7: Criar `Program.cs`**

```csharp
using MovimentacoesFinanceiras.Aplicacao;
using MovimentacoesFinanceiras.Infraestrutura;
using MovimentacoesFinanceiras.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddAplicacao()
    .AddInfraestrutura(builder.Configuration)
    .AddWorker(builder.Configuration);

var app = builder.Build();

await app.Services.AplicarMigracoesAsync();

await app.RunAsync();
```

- [ ] **Step 8: Criar `appsettings.json`**

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "UserName": "guest",
    "Password": "guest"
  },
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=movimentacoes;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

- [ ] **Step 9: Verificar build**

```bash
dotnet build src/MovimentacoesFinanceiras.Worker/MovimentacoesFinanceiras.Worker.csproj
```

Esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 10: Commit**

```bash
git add src/MovimentacoesFinanceiras.Worker/
git commit -m "feat: add Worker RabbitMQ infrastructure, consumer and DI wiring"
```

---

## Task 4: Worker — Dockerfile

**Files:**
- Create: `src/MovimentacoesFinanceiras.Worker/Dockerfile`

- [ ] **Step 1: Criar `Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ ./src/
RUN dotnet restore src/MovimentacoesFinanceiras.Worker/MovimentacoesFinanceiras.Worker.csproj
RUN dotnet publish src/MovimentacoesFinanceiras.Worker/MovimentacoesFinanceiras.Worker.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MovimentacoesFinanceiras.Worker.dll"]
```

> Usa `dotnet/runtime` (não `aspnet`) porque o Worker não expõe HTTP.

- [ ] **Step 2: Commit**

```bash
git add src/MovimentacoesFinanceiras.Worker/Dockerfile
git commit -m "feat: add Worker Dockerfile"
```

---

## Task 5: Produtor — `PublicadorRabbitMq` (TDD)

**Files:**
- Create: `tests/Produtor.Testes/Produtor.Testes.csproj`
- Create: `tests/Produtor.Testes/Mensageria/PublicadorRabbitMqTestes.cs`
- Create: `src/MovimentacoesFinanceiras.Produtor/MovimentacoesFinanceiras.Produtor.csproj`
- Create: `src/MovimentacoesFinanceiras.Produtor/Mensageria/RabbitMqOptions.cs`
- Create: `src/MovimentacoesFinanceiras.Produtor/Mensageria/IConexaoRabbitMq.cs`
- Create: `src/MovimentacoesFinanceiras.Produtor/Mensageria/ConexaoRabbitMq.cs`
- Create: `src/MovimentacoesFinanceiras.Produtor/Mensageria/IPublicadorDeMovimentacoes.cs`
- Create: `src/MovimentacoesFinanceiras.Produtor/Mensageria/PublicadorRabbitMq.cs`

- [ ] **Step 1: Criar `Produtor.Testes.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="coverlet.collector" Version="$(CoverletVersion)" />
        <PackageReference Include="FluentAssertions" Version="$(FluentAssertionsVersion)" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="$(TestSdkVersion)" />
        <PackageReference Include="NSubstitute" Version="$(NSubstituteVersion)" />
        <PackageReference Include="xunit" Version="$(XunitVersion)" />
        <PackageReference Include="xunit.runner.visualstudio" Version="$(XunitRunnerVersion)" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="Xunit" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\src\MovimentacoesFinanceiras.Produtor\MovimentacoesFinanceiras.Produtor.csproj" />
    </ItemGroup>

</Project>
```

- [ ] **Step 2: Criar `PublicadorRabbitMqTestes.cs`**

```csharp
using FluentAssertions;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Produtor.Mensageria;
using NSubstitute;
using RabbitMQ.Client;

namespace Produtor.Testes.Mensageria;

public class PublicadorRabbitMqTestes
{
    private readonly IConexaoRabbitMq _conexao = Substitute.For<IConexaoRabbitMq>();
    private readonly IChannel _canal = Substitute.For<IChannel>();

    public PublicadorRabbitMqTestes()
    {
        _conexao.CriarCanalAsync(Arg.Any<CancellationToken>())
            .Returns(_canal);
    }

    [Fact]
    public async Task PublicarAsync_MensagemValida_ChamaBasicPublishComParametrosCorretos()
    {
        // Arrange
        var publicador = new PublicadorRabbitMq(_conexao);
        var mensagem = new ComandoMovimentacaoMessage(
            ContaId: Guid.NewGuid(),
            Tipo: TipoLancamento.Credito,
            Valor: 200m,
            Descricao: "Teste",
            ChaveIdempotencia: Guid.NewGuid().ToString());

        // Act
        await publicador.PublicarAsync(mensagem, CancellationToken.None);

        // Assert
        await _canal.Received(1).BasicPublishAsync(
            exchange: TopologiaRabbitMq.Exchange,
            routingKey: TopologiaRabbitMq.RoutingKey,
            mandatory: false,
            basicProperties: Arg.Is<BasicProperties>(p =>
                p.Persistent == true &&
                p.MessageId == mensagem.ChaveIdempotencia &&
                p.ContentType == "application/json"),
            body: Arg.Any<ReadOnlyMemory<byte>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublicarAsync_SempreDispoeCanalAoFinalizar()
    {
        // Arrange
        var publicador = new PublicadorRabbitMq(_conexao);
        var mensagem = new ComandoMovimentacaoMessage(
            Guid.NewGuid(), TipoLancamento.Debito, 50m, null, Guid.NewGuid().ToString());

        // Act
        await publicador.PublicarAsync(mensagem, CancellationToken.None);

        // Assert — canal foi descartado (DisposeAsync chamado)
        await _canal.Received(1).DisposeAsync();
    }
}
```

- [ ] **Step 3: Criar `MovimentacoesFinanceiras.Produtor.csproj`** (mínimo para compilar o teste)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <ItemGroup>
        <PackageReference Include="RabbitMQ.Client" Version="$(RabbitMqClientVersion)" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\MovimentacoesFinanceiras.Mensagens\MovimentacoesFinanceiras.Mensagens.csproj" />
    </ItemGroup>

</Project>
```

- [ ] **Step 4: Rodar os testes para confirmar que falham**

```bash
dotnet test tests/Produtor.Testes/Produtor.Testes.csproj
```

Esperado: erros de compilação — `IConexaoRabbitMq`, `PublicadorRabbitMq` não existem ainda.

- [ ] **Step 5: Criar `RabbitMqOptions.cs`**

```csharp
namespace MovimentacoesFinanceiras.Produtor.Mensageria;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
```

- [ ] **Step 6: Criar `IConexaoRabbitMq.cs`**

```csharp
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Produtor.Mensageria;

public interface IConexaoRabbitMq
{
    Task<IChannel> CriarCanalAsync(CancellationToken ct = default);
}
```

- [ ] **Step 7: Criar `ConexaoRabbitMq.cs`**

```csharp
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Produtor.Mensageria;

public sealed class ConexaoRabbitMq : IConexaoRabbitMq, IDisposable
{
    private readonly IConnection _conexao;

    private ConexaoRabbitMq(IConnection conexao) => _conexao = conexao;

    public static async Task<ConexaoRabbitMq> CriarAsync(RabbitMqOptions opcoes, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(opcoes);

        var factory = new ConnectionFactory
        {
            HostName = opcoes.Host,
            Port = opcoes.Port,
            VirtualHost = opcoes.VirtualHost,
            UserName = opcoes.UserName,
            Password = opcoes.Password,
            AutomaticRecoveryEnabled = true
        };

        var conexao = await factory.CreateConnectionAsync(ct);
        return new ConexaoRabbitMq(conexao);
    }

    public async Task<IChannel> CriarCanalAsync(CancellationToken ct = default) =>
        await _conexao.CreateChannelAsync(cancellationToken: ct);

    public void Dispose() => _conexao.Dispose();
}
```

- [ ] **Step 8: Criar `IPublicadorDeMovimentacoes.cs`**

```csharp
using MovimentacoesFinanceiras.Mensagens;

namespace MovimentacoesFinanceiras.Produtor.Mensageria;

public interface IPublicadorDeMovimentacoes
{
    Task PublicarAsync(ComandoMovimentacaoMessage mensagem, CancellationToken ct = default);
}
```

- [ ] **Step 9: Criar `PublicadorRabbitMq.cs`**

```csharp
using System.Text.Json;
using MovimentacoesFinanceiras.Mensagens;
using RabbitMQ.Client;

namespace MovimentacoesFinanceiras.Produtor.Mensageria;

public sealed class PublicadorRabbitMq(IConexaoRabbitMq conexao) : IPublicadorDeMovimentacoes
{
    public async Task PublicarAsync(ComandoMovimentacaoMessage mensagem, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mensagem);

        await using var canal = await conexao.CriarCanalAsync(ct);

        var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem);

        await canal.BasicPublishAsync(
            exchange: TopologiaRabbitMq.Exchange,
            routingKey: TopologiaRabbitMq.RoutingKey,
            mandatory: false,
            basicProperties: new BasicProperties
            {
                Persistent = true,
                MessageId = mensagem.ChaveIdempotencia,
                ContentType = "application/json"
            },
            body: corpo,
            cancellationToken: ct);
    }
}
```

- [ ] **Step 10: Rodar os testes para confirmar que passam**

```bash
dotnet test tests/Produtor.Testes/Produtor.Testes.csproj --logger "console;verbosity=normal"
```

Esperado: `2 passed, 0 failed`.

- [ ] **Step 11: Commit**

```bash
git add src/MovimentacoesFinanceiras.Produtor/ tests/Produtor.Testes/
git commit -m "feat: add Produtor project with PublicadorRabbitMq (TDD)"
```

---

## Task 6: Produtor — DependencyInjection + Program.cs + Dockerfile

**Files:**
- Create: `src/MovimentacoesFinanceiras.Produtor/DependencyInjection.cs`
- Create: `src/MovimentacoesFinanceiras.Produtor/Program.cs`
- Create: `src/MovimentacoesFinanceiras.Produtor/appsettings.json`
- Create: `src/MovimentacoesFinanceiras.Produtor/Dockerfile`

- [ ] **Step 1: Criar `DependencyInjection.cs`**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovimentacoesFinanceiras.Produtor.Mensageria;

namespace MovimentacoesFinanceiras.Produtor;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProdutor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var opcoes = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddSingleton<IConexaoRabbitMq>(_ =>
            ConexaoRabbitMq.CriarAsync(opcoes).GetAwaiter().GetResult());

        services.AddScoped<IPublicadorDeMovimentacoes, PublicadorRabbitMq>();

        return services;
    }
}
```

- [ ] **Step 2: Criar `Program.cs`**

```csharp
using System.Text.Json.Serialization;
using MovimentacoesFinanceiras.Dominio.Contas;
using MovimentacoesFinanceiras.Mensagens;
using MovimentacoesFinanceiras.Produtor;
using MovimentacoesFinanceiras.Produtor.Mensageria;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProdutor(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.MapPost("/movimentacoes", async (
    PublicarMovimentacaoRequest request,
    IPublicadorDeMovimentacoes publicador,
    CancellationToken ct) =>
{
    ArgumentNullException.ThrowIfNull(request);

    var chaveIdempotencia = Guid.NewGuid().ToString();

    var mensagem = new ComandoMovimentacaoMessage(
        request.ContaId,
        request.Tipo,
        request.Valor,
        request.Descricao,
        chaveIdempotencia);

    await publicador.PublicarAsync(mensagem, ct);

    return Results.Accepted(value: new { chaveIdempotencia });
});

await app.RunAsync();

public record PublicarMovimentacaoRequest(
    Guid ContaId,
    TipoLancamento Tipo,
    decimal Valor,
    string? Descricao);
```

- [ ] **Step 3: Criar `appsettings.json`**

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "UserName": "guest",
    "Password": "guest"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- [ ] **Step 4: Criar `Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ ./src/
RUN dotnet restore src/MovimentacoesFinanceiras.Produtor/MovimentacoesFinanceiras.Produtor.csproj
RUN dotnet publish src/MovimentacoesFinanceiras.Produtor/MovimentacoesFinanceiras.Produtor.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MovimentacoesFinanceiras.Produtor.dll"]
```

- [ ] **Step 5: Verificar build completo da solution**

```bash
dotnet build
```

Esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 6: Rodar todos os testes**

```bash
dotnet test
```

Esperado: todos os testes passando (incluindo os existentes de domínio e API).

- [ ] **Step 7: Commit**

```bash
git add src/MovimentacoesFinanceiras.Produtor/
git commit -m "feat: add Produtor DI, minimal API endpoint and Dockerfile"
```

---

## Task 7: docker-compose — RabbitMQ + Worker + Produtor

**Files:**
- Modify: `docker-compose.yml`

- [ ] **Step 1: Atualizar `docker-compose.yml`**

Substitua o conteúdo completo do arquivo:

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: movimentacoes
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d movimentacoes"]
      interval: 5s
      timeout: 3s
      retries: 5

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: src/MovimentacoesFinanceiras.Api/Dockerfile
    environment:
      ConnectionStrings__Postgres: "Host=postgres;Port=5432;Database=movimentacoes;Username=postgres;Password=postgres"
      ApiKeys__0: "dev-key-local-somente"
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy

  worker:
    build:
      context: .
      dockerfile: src/MovimentacoesFinanceiras.Worker/Dockerfile
    environment:
      ConnectionStrings__Postgres: "Host=postgres;Port=5432;Database=movimentacoes;Username=postgres;Password=postgres"
      RabbitMq__Host: rabbitmq
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy

  produtor:
    build:
      context: .
      dockerfile: src/MovimentacoesFinanceiras.Produtor/Dockerfile
    environment:
      RabbitMq__Host: rabbitmq
    ports:
      - "8081:8080"
    depends_on:
      rabbitmq:
        condition: service_healthy

volumes:
  pgdata:
```

- [ ] **Step 2: Validar sintaxe do compose**

```bash
docker compose config
```

Esperado: saída do compose expandido sem erros.

- [ ] **Step 3: Commit**

```bash
git add docker-compose.yml
git commit -m "feat: add RabbitMQ, Worker and Produtor services to docker-compose"
```

---

## Verificação Final

- [ ] **Build completo**

```bash
dotnet build -warnaserror
```

Esperado: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Todos os testes**

```bash
dotnet test
```

Esperado: todos os testes passando.

- [ ] **Smoke test docker-compose**

```bash
# Subir tudo
docker compose up --build -d

# Aguardar ~15s para estabilização

# Criar uma conta via API
curl -s -X POST http://localhost:8080/contas \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-key-local-somente" \
  -d '{"clienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}' | jq .

# Copiar o "id" retornado e substituir em <CONTA_ID>

# Publicar via Produtor (caminho assíncrono)
curl -s -X POST http://localhost:8081/movimentacoes \
  -H "Content-Type: application/json" \
  -d '{"contaId": "<CONTA_ID>", "tipo": "Credito", "valor": 250.00, "descricao": "Via fila"}' | jq .

# Aguardar ~2s para o Worker processar

# Confirmar saldo via API (caminho síncrono)
curl -s http://localhost:8080/contas/<CONTA_ID>/saldo \
  -H "X-Api-Key: dev-key-local-somente" | jq .
# Esperado: "saldoAtual": 250.00

# Inspecionar filas no management UI
# http://localhost:15672 — login: guest / guest
```
