# ADR-019 — HTTPS + HSTS

**Status:** Aceito
**Data:** 2026-09-23

## Contexto

Dados financeiros trafegando em texto claro são inaceitáveis. Havia um perfil
`https` no `launchSettings.json`, mas o `Program.cs` não aplicava
`UseHttpsRedirection` nem HSTS — na prática o tráfego podia seguir em HTTP.

## Decisão

Aplicar `app.UseHttpsRedirection()` e `app.UseHsts()` (este fora de Development).
Alinhar Kestrel/launchSettings e ajustar o README para o endpoint HTTPS.

## Alternativas consideradas

- **Terminar TLS só no gateway/ingress** — comum em produção com reverse proxy,
  mas deixa a aplicação sem garantia em execução local/standalone; combinar defesa
  em profundidade é preferível.
- **Não fazer nada** — mantém o risco de tráfego em claro.

## Consequências

- Tráfego cifrado por padrão; HSTS instrui o cliente a sempre usar HTTPS.
- Em produção atrás de proxy, `UseHttpsRedirection` coopera com `ForwardedHeaders`
  (documentado como ajuste de deploy).
- Custo: certificado de desenvolvimento (`dotnet dev-certs`) para rodar local.
