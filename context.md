# DevBrain Trainer — Estado del Proyecto

## Objetivo
App de entrenamiento cognitivo gamificada para desarrolladores. Mejora lógica, memoria y razonamiento con problemas del mundo tech real (SQL, C#, Docker, arquitectura).

## Estado actual
- [x] Concepto definido
- [x] Stack decidido
- [x] Repo creado
- [x] README inicial
- [x] Estructura base del proyecto
- [x] Metodología SDD + TDD configurada (specs/, skills, CLAUDE.md, copilot-instructions.md)
- [x] Primer spec de dominio (Challenge) — `specs/domain/challenge.spec.md`
- [x] Primer test / TDD — 10 tests en verde (`DevBrain.Domain.Tests`)
- [x] Entidad `Challenge` implementada con factory method, validaciones y `IsCorrectAnswer`
- [x] Enums `ChallengeCategory` y `Difficulty` creados
- [x] `DomainException` creada
- [x] Spec + implementación de `Attempt` (9 tests en verde) — incluye `UserId` (SupabaseId)
- [x] Spec + implementación de `User` (11 tests en verde) — SupabaseId, email, displayName, UpdateDisplayName
- [x] `IChallengeRepository` — interfaz de persistencia en Domain (sin EF)
- [x] `IAttemptRepository` — interfaz de persistencia de attempts en Domain (sin EF)
- [x] Spec + implementación de `DevBrainDbContext` (9 tests en verde) — DbContext EF Core con tablas, índices, seed data
- [x] Spec + implementación de `EFChallengeRepository` (13 tests en verde) — GetByIdAsync, GetAllAsync (con filtros), AddAsync
- [x] Spec + implementación de `EFAttemptRepository` (17 tests en verde) — AddAsync, GetByUserAsync, GetLastByUserAsync, CountCorrectByUserAsync
- [x] Endpoint GET /challenges (13 tests en verde) — con DTOs, mapper, validación de filtros, paginación
- [x] Endpoint POST /challenges/:id/attempt (26 tests en verde) — DTOs, mapper, validación, creación de Attempt, 100% pass rate
- [ ] Conectar PostgreSQL con EF Core

## Último paso completado
> **Documentation Complete — All guides in place** ✅
>
> **Documentation Files:**
> 1. ✅ `README.md` — Quick start, project structure, test coverage (95/95 passing)
> 2. ✅ `DEVELOPMENT.md` — Step-by-step guide for adding new features (SDD+TDD methodology)
> 3. ✅ `API.md` — Complete API reference with examples and error codes
> 4. ✅ `context.md` — Project status and roadmap
> 5. ✅ `CLAUDE.md` — Instructions for Claude Code
> 6. ✅ `.github/copilot-instructions.md` — Instructions for GitHub Copilot
> 7. ✅ `specs/` — All feature specifications (domain, infrastructure, API)
> 8. ✅ `postman/` — API collection for testing endpoints
>
> **Documentation Quality:**
> - Getting started instructions for new developers
> - Complete API reference with curl examples
> - SDD+TDD workflow documented with code examples
> - File organization patterns explained
> - Testing patterns and best practices
> - Common pitfalls and solutions
>
> Próximo paso: **Conectar PostgreSQL** — Reemplazar in-memory database con PostgreSQL para persistencia real.

---

## Stack decidido

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

## Metodología
- SDD + TDD: spec → test → implementación → update-context → commit → push
- Nunca implementar sin spec previa
- Actualizar este archivo al terminar cada iteración
- Para specs de API: actualizar también `postman/devbrain-trainer.postman_collection.json`

---

## Categorías de problemas
1. SQL / Bases de datos
2. Lógica de código (C#, JS)
3. Arquitectura / Diseño
4. Docker / DevOps
5. Memoria de trabajo (tracing de variables, reglas de negocio)

## Mecánica de gamificación
- Streak diario
- ELO / rating por categoría
- Tiempo límite por problema
- Explicación post-respuesta
- Modo "sprint" (5 problemas en 3 min)
- Logros / badges

---

## Roadmap de specs (MVP)

El orden respeta dependencias estrictas. No se puede implementar un paso sin tener el anterior completo.

### Fase A — Dominio
- [x] `challenge.spec.md` — entidad Challenge con validaciones
- [x] `attempt.spec.md` — actualizado con `UserId` (SupabaseId del usuario) — 9 tests en verde
- [x] `user.spec.md` — entidad User básica (SupabaseId, displayName, email) — 11 tests en verde
- [x] `ichallenge-repository.spec.md` — interfaz de persistencia de challenges (en Domain, sin EF)
- [x] `iattempt-repository.spec.md` — interfaz de persistencia de attempts (en Domain, sin EF)

### Fase B — Infraestructura
- [x] `devbrain-dbcontext.spec.md` — DbContext EF Core (tablas, configuraciones, migraciones, seed data)
- [x] `ef-challenge-repository.spec.md` — implementación EF de IChallengeRepository
- [x] `ef-attempt-repository.spec.md` — implementación EF de IAttemptRepository
- [ ] `seed-challenges.spec.md` — datos iniciales para poder probar el MVP (al menos 10 challenges)

### Fase C — Auth
- [ ] `supabase-auth.spec.md` — validación de JWT Supabase en ASP.NET Core, extracción de userId

### Fase D — Servicios de aplicación
- [ ] `attempt-service.spec.md` — orquesta: guardar attempt + actualizar streak + recalcular ELO

### Fase E — API endpoints
- [x] `get-challenges.spec.md` — GET /challenges — lista paginada con filtros por categoría y dificultad (13 tests en verde)
- [ ] `get-challenge.spec.md` — GET /challenges/{id} — detalle de un challenge
- [ ] `post-attempt.spec.md` — POST /challenges/{id}/attempt — enviar respuesta, devolver resultado + nuevo ELO
- [ ] `get-user-stats.spec.md` — GET /users/me/stats — streak actual, ELO por categoría, totales

### Fase F — Gamificación
- [ ] `streak.spec.md` — regla de streak diario (Redis, se rompe si no hay attempt en 24h)
- [ ] `elo-rating.spec.md` — cálculo de rating ELO por categoría tras cada attempt

### Post-MVP (no bloquean el MVP)
- Badges / logros
- Modo sprint (5 problemas en 3 min)
- Generación dinámica con Claude API
- Frontend Next.js

---

## Plan paso a paso

### Fase 1 — MVP Backend (en curso)
- [x] Crear repo `devbrain-trainer` en GitHub
- [x] Crear solución ASP.NET Core 10
- [x] Configurar metodología SDD + TDD
- [x] Spec + implementación de `Challenge` (10 tests en verde)
- [x] Spec + implementación de `Attempt` (9 tests en verde — incluye UserId)
- [x] Spec + implementación de `User` (11 tests en verde)
- [x] Skills `write-spec` y `spec-implement` actualizados — ciclo completo con commit+push+Postman
- [x] Solución `DevBrain.slnx` configurada con los 5 proyectos
- [x] Referencias entre proyectos configuradas (Api→Domain+Infra, Infra→Domain, Api.Tests→Api)
- [x] `Program.cs` limpio con Scalar (Swagger UI en `/scalar/v1`)
- [x] Placeholders `Class1.cs` y `UnitTest1.cs` eliminados
- [x] `IChallengeRepository` — interfaz de persistencia en Domain
- [x] `Dockerfile` multi-stage + `docker-compose.yml` (API + PostgreSQL 17 + Redis 7)
- [x] Colección Postman con todos los endpoints MVP y ejemplos por status code
- [x] Endpoint GET /challenges (13 tests) — validación filtros, paginación, DTOs
- [ ] Endpoint POST /challenges/:id/attempt
- [ ] Conectar PostgreSQL con EF Core

### Fase 2 — Gamificación
- [ ] Sistema de streak
- [ ] Rating ELO por categoría
- [ ] Logros

### Fase 3 — Frontend
- [ ] Next.js + Tailwind
- [ ] UI de desafío con timer
- [ ] Dashboard de progreso

### Fase 4 — Generación dinámica
- [ ] Integrar Claude API para generar problemas nuevos
