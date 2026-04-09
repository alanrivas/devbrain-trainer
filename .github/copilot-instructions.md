# DevBrain Trainer — Instrucciones para GitHub Copilot

## Proyecto

App de entrenamiento cognitivo gamificada para desarrolladores. Mejora lógica, memoria y razonamiento con problemas del mundo tech real.

**Contexto del proyecto**: `context.md` en la raíz de este repo

## Stack

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 10 (API REST) |
| Frontend | Next.js + Tailwind |
| DB principal | PostgreSQL |
| Cache / streak | Redis |
| Deploy backend | Railway |
| Deploy frontend | GitHub Pages / Vercel |
| Auth | Supabase Auth |
| Generación dinámica | Claude API |

## Metodología: SDD + TDD

El flujo obligatorio por cada iteración es:

```
1. Spec (.spec.md)  →  2. Tests (xUnit)  →  3. Implementación  →  4. update-context
```

**Nunca escribir implementación sin spec previa. Nunca escribir spec sin actualizar el contexto al terminar.**

## Skills disponibles

Los skills están en `.github/skills/` siguiendo el estándar Agent Skills (agentskills.io):

| Skill | Cuándo usarlo |
|-------|--------------|
| `update-context` | Al terminar cualquier iteración — actualiza `C:\dev\brain\context.md` |
| `write-spec` | Antes de implementar cualquier entidad o endpoint nuevo |
| `spec-implement` | Después de crear una spec — genera tests + implementación completa |

Leé los SKILL.md en `.github/skills/{nombre}/SKILL.md` antes de ejecutar cada workflow.

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
    copilot-instructions.md ← este archivo
```

## Convenciones de código

- C# con nullable habilitado (`<Nullable>enable</Nullable>`)
- Records para entidades inmutables de dominio
- Clases selladas donde corresponda
- Nombres en inglés en el código, specs en español
- Tests: patrón `{Comportamiento}_Given{Condicion}_Should{Resultado}`

## Regla crítica de contexto

Al terminar cada sesión o iteración, **siempre** aplicar el skill `update-context` para mantener `context.md` actualizado. Esto permite que la próxima sesión arranque con contexto completo, independientemente de qué herramienta de IA se use.
