#!/bin/bash
set -e
cd /home/thiagots/desafio-banco
export PATH="/home/thiagots/.dotnet:/home/thiagots/.dotnet/tools:$PATH"

# Commit 1: Handler de API Key
git add \
  src/MovimentacoesFinanceiras.Api/Autenticacao/ApiKeyOptions.cs \
  src/MovimentacoesFinanceiras.Api/Autenticacao/ApiKeyAuthenticationHandler.cs
git commit -m "feat(api): handler de autenticação por API Key"

# Commit 2: Registrar auth e proteger endpoints
git add \
  src/MovimentacoesFinanceiras.Api/DependencyInjection.cs \
  src/MovimentacoesFinanceiras.Api/Controllers/ContasController.cs \
  src/MovimentacoesFinanceiras.Api/appsettings.json \
  src/MovimentacoesFinanceiras.Api/appsettings.Development.json \
  src/MovimentacoesFinanceiras.Api/Program.cs
git commit -m "feat(api): autenticação API Key registrada + [Authorize] + Swagger security + HTTPS/HSTS"

# Commit 3: Testes de integração com API Key + 401
git add \
  tests/Api.Testes/AplicacaoFactory.cs \
  tests/Api.Testes/Contas/ContasIntegracaoTestes.cs \
  tests/Api.Testes/Contas/ContasConcorrenciaTestes.cs
git commit -m "test(api): API Key nos testes de integração + caso 401"

# Commit 4: Redaction de campos sensíveis (Serilog)
git add src/MovimentacoesFinanceiras.Api/Logging/RedacaoDadosSensiveis.cs
git commit -m "feat(api): redaction de campos sensíveis nos logs (Serilog policy)"

# Commit 5: Secrets + README
git add README.md
git commit -m "chore(seguranca): segredos via user-secrets/env; appsettings sem valores sensíveis; README atualizado"

git log --oneline -6
