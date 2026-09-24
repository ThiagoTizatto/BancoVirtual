using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using MovimentacoesFinanceiras.Api.Autenticacao;
using OpenTelemetry.Metrics;
using System.Text.Json.Serialization;

namespace MovimentacoesFinanceiras.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddProblemDetails();

        services.AddAuthentication(ApiKeyOptions.Esquema)
            .AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(
                ApiKeyOptions.Esquema,
                opcoes =>
                {
                    var chaves = configuration.GetSection("ApiKeys").Get<string[]>() ?? [];
                    opcoes.ChavesValidas = chaves;
                });

        services.AddAuthorization();

        services.AddControllers()
            .AddJsonOptions(opcoes =>
                opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opcoes =>
        {
            opcoes.SwaggerDoc("v1", new()
            {
                Title = "API de Movimentações Financeiras",
                Description = "Registra movimentações financeiras e consulta saldos de contas bancárias. " +
                              "Utiliza CQRS com ledger append-only para rastreabilidade completa.",
                Version = "v1"
            });

            opcoes.AddSecurityDefinition(ApiKeyOptions.Esquema, new OpenApiSecurityScheme
            {
                Name = ApiKeyOptions.NomeCabecalho,
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Description = "Informe a API Key no header X-Api-Key."
            });

            opcoes.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = ApiKeyOptions.Esquema
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddOpenTelemetry()
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddMeter(MovimentacoesFinanceiras.Aplicacao.Metricas.MetricasMovimentacao.NomeMeter)
                .AddPrometheusExporter());

        services.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opcoes.AddPolicy("por-api-key", contexto =>
            {
                var chave = contexto.Request.Headers["X-Api-Key"].FirstOrDefault()
                    ?? contexto.Connection.RemoteIpAddress?.ToString()
                    ?? "anonimo";

                return RateLimitPartition.GetFixedWindowLimiter(chave, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromSeconds(10),
                    QueueLimit = 0
                });
            });
        });

        return services;
    }
}
