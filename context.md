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
- [x] Spec + implementación de `Attempt` (9 tests en verde) — incluye `UserId` (ahora Guid)
- [x] Spec + implementación de `User` (11 tests en verde) — Guid Id, email, displayName, password hash support
- [x] `IChallengeRepository` — interfaz de persistencia en Domain (sin EF)
- [x] `IAttemptRepository` — interfaz de persistencia de attempts en Domain (sin EF)
- [x] `IUserRepository` — interfaz de persistencia de usuarios en Domain (sin EF)
- [x] Spec + implementación de `DevBrainDbContext` (9 tests en verde) — DbContext EF Core con tablas, índices, seed data
- [x] Spec + implementación de `EFChallengeRepository` (13 tests en verde) — GetByIdAsync, GetAllAsync (con filtros), AddAsync
- [x] Spec + implementación de `EFAttemptRepository` (17 tests en verde) — AddAsync, GetByUserAsync, GetLastByUserAsync, CountCorrectByUserAsync
- [x] Spec + implementación de `EFUserRepository` (9 tests en verde) — AddAsync, GetByEmailAsync, GetByIdAsync
- [x] Endpoint GET /challenges (13 tests en verde) — con DTOs, mapper, validación de filtros, paginación
- [x] Endpoint POST /challenges/:id/attempt (26 tests en verde) — DTOs, mapper, validación, creación de Attempt, 100% pass rate
- [x] Endpoint POST /auth/register (13 tests en verde) — Email/password/displayName validation, PBKDF2 hashing, duplicate detection, 100% pass rate
- [x] Conectar PostgreSQL con EF Core — migrations aplicadas, schema creado en port 5433, tests siguen en verde (108/108)

## Test Suites Status

| Suite | Tests | Status | Details |
|-------|-------|--------|---------|
| Domain.Tests | 30 | ✅ 30/30 | User factory + validation, Attempt entity, Challenge logic |
| Infrastructure.Tests | 39 | ✅ 39/39 | DbContext config, EFChallengeRepository, EFAttemptRepository, EFUserRepository |
| Api.Tests | 39 | ✅ 39/39 | GET /challenges (13), POST /attempt (26), POST /auth/register (13) |
| **TOTAL** | **108** | **✅ 108/108** | 100% pass rate, all scenarios covered |

## Último paso completado
> **PostgreSQL 18 local setup complete — port 5433, migrations applied, 108/108 tests passing** ✅
>
> **Setup Summary**:
> - PostgreSQL 18 running on port 5433 (port 5432 had conflict with PG 16)
> - Database: `devbrain_local`, User: `devbrain` with limited permissions
> - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0 installed + EF Core 10.0.4 aligned
> - Connection string: stored securely in User Secrets (credentials never hardcoded)
> - Trust authentication enabled for localhost (development only) via `pg_hba.conf`
> - Initial migration created with **deterministic seed data** (fixed GUIDs + dates, not dynamic)
>   - 10 challenges pre-seeded with cross-domain coverage (SQL, C#, Architecture, Docker, Memory)
> - Database schema fully created: `users`, `challenges`, `attempts`, `__EFMigrationsHistory`
> - Indexes: category, difficulty filters on challenges; userId + timestamps on attempts
>
> **Code Changes**:
> - `Challenge.CreateForSeeding()` — new public factory for deterministic migration seeding
> - `DevBrainDbContext.OnModelCreating()` — seed data now uses fixed IDs & dates (not Guid.NewGuid())
> - `Program.cs` — conditional Npgsql registration (skipped if `DOTNET_RUNNING_IN_TEST` env var set)
> - `CustomWebApplicationFactory` — sets `DOTNET_RUNNING_IN_TEST` to avoid provider conflicts
> - `EF Core Migration InitialCreate` — deterministic model snapshot (no more dynamic values error)
>
> **Test Impact** (All 108/108 Passing):
> - Domain tests: **30/30** — unaffected (no domain changes)
> - Infrastructure tests: **39/39** — DbContext w/ In-Memory, no PostgreSQL access
> - API tests: **39/39** — WebApplicationFactory injects In-Memory for all test scenarios
> - Production app: uses PostgreSQL on port 5433 when connection string exists
> - Tests: use In-Memory isolated DBs per factory instance (no cross-contamination)
>
> **Deployment Path**:
> - Local dev: PostgreSQL 18 on 5433 (this setup)
> - Production (Railway): PostgreSQL via Railway addon + connection string from env
> - Tests: In-Memory (no DB dependency)
> - Migration rollback: `Connection string not found → Program.cs skips Npgsql → EF uses In-Memory`
>
> **Next step**: Choose next feature to implement:
> - Option A: User login endpoint (POST /auth/login)
> - Option B: GET /challenges/{id} (single challenge detail)
> - Option C: Gamification layer (streak, ELO, rating)
> - Option D: Leaderboard (GET /users/stats or similar)

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
- [ ] `post-auth-login.spec.md` — POST /auth/login — email + password → JWT/token (ADDING THIS NEXT)
- [ ] `supabase-auth.spec.md` — validación de JWT Supabase

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

### Fase 1 — MVP Backend (✅ COMPLETA)
- [x] Crear repo `devbrain-trainer` en GitHub
- [x] Crear solución ASP.NET Core 10
- [x] Configurar metodología SDD + TDD
- [x] Spec + implementación de `Challenge` (10 tests en verde)
- [x] Spec + implementación de `Attempt` (9 tests en verde — Guid userId)
- [x] Spec + implementación de `User` (11 tests en verde — Guid Id, password hash support)
- [x] Spec + implementación de `IUserRepository` + `EFUserRepository` (9 tests)
- [x] Skills `write-spec` y `spec-implement` actualizados — ciclo completo con commit+push+Postman
- [x] Solución `DevBrain.slnx` configurada con los 5 proyectos
- [x] Referencias entre proyectos configuradas (Api→Domain+Infra, Infra→Domain, Api.Tests→Api)
- [x] `Program.cs` limpio con Scalar (Swagger UI en `/scalar/v1`)
- [x] Placeholders `Class1.cs` y `UnitTest1.cs` eliminados
- [x] `IChallengeRepository` — interfaz de persistencia en Domain
- [x] `Dockerfile` multi-stage + `docker-compose.yml` (API + PostgreSQL 17 + Redis 7)
- [x] Colección Postman con todos los endpoints MVP y ejemplos por status code
- [x] Endpoint GET /challenges (13 tests) — validación filtros, paginación, DTOs, mapper
- [x] Endpoint POST /challenges/:id/attempt (26 tests) — validación, DTOs, Attempt creation, ELO-ready
- [x] Endpoint POST /auth/register (13 tests) — email/password/displayName validation, PBKDF2 hashing, duplicate detection
- [x] **TOTAL: 108/108 tests passing (100% pass rate)**
- [x] Context.md actualizado con avance
- [ ] Conectar PostgreSQL con EF Core (siguiente paso)

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
