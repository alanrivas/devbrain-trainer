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
- [ ] Conectar PostgreSQL con EF Core

## Test Suites Status

| Suite | Tests | Status | Details |
|-------|-------|--------|---------|
| Domain.Tests | 30 | ✅ 30/30 | User factory + validation, Attempt entity, Challenge logic |
| Infrastructure.Tests | 39 | ✅ 39/39 | DbContext config, EFChallengeRepository, EFAttemptRepository, EFUserRepository |
| Api.Tests | 39 | ✅ 39/39 | GET /challenges (13), POST /attempt (26), POST /auth/register (13) |
| **TOTAL** | **108** | **✅ 108/108** | 100% pass rate, all scenarios covered |

## Último paso completado
> **POST /auth/register fully implemented & 108/108 tests passing** ✅
>
> **Implementation Summary**:
> - Spec: `specs/api/post-auth-register.spec.md` (259 lines) ✅
> - Tests: `PostAuthRegisterEndpointTests.cs` (13 tests) — 100% pass rate ✅
> - Handler: `AuthEndpoints.RegisterAsync()` with full validation ✅
> - Domain: `User.CreateFromRegistration()` factory with email/password/displayName validation ✅
> - Repos: `IUserRepository` + `EFUserRepository` with case-insensitive email lookup ✅
> - Services: `IPasswordHashService` + PBKDF2 hashing (100k iterations) ✅
> - DTOs: `RegisterRequestDto`, `UserResponseDto` + mapper ✅
>
> **What Changed**:
> - User.Id: string → Guid (server-generated)
> - Attempt.UserId: string → Guid (consistency with User)
> - Email validation: format, uniqueness (case-insensitive)
> - Password validation: 8+ chars, requires uppercase + digit
> - DisplayName validation: 3-50 chars, alphanumeric + spaces/dash/dot
> - Error responses: 400 (validation), 409 (duplicate email)
>
> **Test Coverage**:
> - Domain: 30/30 ✅
> - Infrastructure: 39/39 ✅
> - API: 39/39 (26 attempt + 13 registration) ✅
> - **TOTAL: 108/108 ✅**
>
> **Ready for**: PostgreSQL connection or next endpoint (user login, leaderboard, etc)

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
