# DevBrain Trainer — Instrucciones para Claude Code

## Proyecto

App de entrenamiento cognitivo gamificada para desarrolladores. Mejora lógica, memoria y razonamiento con problemas del mundo tech real.

**Contexto del proyecto**: `context.md` en la raíz de este repo

## Stack

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 10 (API REST) |
| Frontend | Next.js + Tailwind |
| DB principal | PostgreSQL (Neon en prod) |
| Cache / streak | Redis (Redis Cloud en prod) |
| Deploy backend | **Azure App Service** |
| Deploy frontend | GitHub Pages / Vercel |
| Logging estructurado | Serilog + Application Insights |
| Auth | JWT propio (plans: Supabase) |
| Generación dinámica | Claude API |

## Metodología: SDD + TDD

El flujo obligatorio por cada iteración es:

```
1. Spec (.spec.md)  →  2. Tests (xUnit)  →  3. Implementación  →  4. update-context
```

**Nunca escribir implementación sin spec previa. Nunca escribir spec sin actualizar el contexto al terminar.**

## Protocolo: Validación de Sesión Inicial

**SIEMPRE que inicies una sesión nueva** (después que la anterior cerró por token limit):

1. Ejecuta agente `session-validator` (ver `.github/agents/session-validator/AGENT.md`):
   - Valida estado real vs context.md
   - Reporta sincronización
   - Sugiere próximo paso

2. Espera resultado del agente

3. Una vez validado, continúa con trabajo según contexto reportado

**Referencia**: Ver `docs/AGENTS-SKILLS-REFERENCE.md` para detalles completos de todos los agentes y skills disponibles.

## Skills disponibles

Los skills están en `.github/skills/` y funcionan como comandos estructurados:

| Skill | Cuándo usarlo |
|-------|--------------|
| `update-context` | Al terminar cualquier iteración — actualiza `context.md` en la raíz del repo |
| `write-spec` | Antes de implementar cualquier entidad o endpoint nuevo |
| `spec-implement` | Después de crear una spec — genera tests + implementación + commit + push |

Leé siempre los SKILL.md correspondientes antes de ejecutar el workflow.

## Estructura del proyecto

```
devbrain-trainer/
  src/
    DevBrain.Api/          ← ASP.NET Core 10 Web API
    DevBrain.Domain/       ← Entidades, lógica de dominio
    DevBrain.Infrastructure/ ← EF Core, repositorios, servicios externos
  tests/
    DevBrain.Api.Tests/    ← xUnit (assertions nativas)
    DevBrain.Domain.Tests/ ← xUnit (assertions nativas)
  specs/
    domain/                ← specs de entidades
    api/                   ← specs de endpoints
    gamification/          ← specs de reglas de negocio
  .github/
    skills/                ← skills del proyecto (estándar Agent Skills)
```

## Convenciones de código

- C# con nullable habilitado (`<Nullable>enable</Nullable>`)
- Records para entidades inmutables de dominio
- Clases selladas donde corresponda
- Nombres en inglés en el código, specs en español
- Tests: patrón `{Comportamiento}_Given{Condicion}_Should{Resultado}`

## Regla crítica de contexto

Al terminar cada sesión o iteración, **siempre** ejecutar el skill `update-context` para mantener `context.md` actualizado. Esto permite que la próxima sesión (con Claude Code, Copilot, o cualquier otra herramienta) arranque con contexto completo.
