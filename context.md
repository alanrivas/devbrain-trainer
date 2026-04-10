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
- [x] Endpoint POST /challenges/:id/attempt (26 tests en verde) — DTOs, mapper, validación, creación de Attempt, 100% pass rate — **ahora protegido con JWT**
- [x] Endpoint POST /auth/register (13 tests en verde) — Email/password/displayName validation, PBKDF2 hashing, duplicate detection, 100% pass rate
- [x] Conectar PostgreSQL con EF Core — migrations aplicadas, schema creado en port 5433, tests siguen en verde (108/108)

## Test Suites Status

| Suite | Tests | Status | Details |
|-------|-------|--------|---------|
| Domain.Tests | 42 | ✅ 42/42 | User factory + validation, Attempt entity, Challenge logic, EloRatingService (12) |
| Infrastructure.Tests | 39 | ✅ 39/39 | DbContext config (9), EFChallengeRepository (13), EFAttemptRepository (17) — EFUserRepository cubierto por API tests |
| Api.Tests | 77 | ✅ 77/77 | GET /challenges (13), GET /challenges/{id} (8), POST /attempt (26), POST /auth/register (13), POST /auth/login (11), JWT middleware (9), GET /users/me/stats (10) |
| **TOTAL** | **158** | **✅ 158/158** | 100% pass rate |

## Último paso completado
> ✅ **EloRatingService implemented** — 12 tests passing, **158/158 total**
>
> **Implementation Details**:
> - Servicio puro de dominio: `IEloRatingService` + `EloRatingService` en `DevBrain.Domain.Services`
> - Fórmula: ELO adaptado — expected probability + score + time modifier (1.0–1.25) + floor 100
> - Constantes: K=32, Easy=800, Medium=1200, Hard=1600, initial=1000
> - Time modifier solo aplica en respuestas correctas; incorrecto siempre modifier=1.0
> - 12 tests: correctness por dificultad, time modifier, floor de rating, valores exactos anclados
> - Registrar como Singleton en Program.cs (próximo paso al integrar con AttemptService)
>
> **Previous step — GET /users/me/stats**:
> - Endpoint: `GET /api/v1/users/me/stats` — requiere JWT, lee userId desde claims
> - Stats: totalAttempts, correctAttempts, accuracyRate calculados desde `IAttemptRepository`
> - displayName obtenido desde `IUserRepository.GetByIdAsync`
> - Placeholders: `currentStreak = 0`, `eloRating = 1000` (hasta Fase F)
> - Parallel fetch: `GetByUserAsync` + `CountCorrectByUserAsync` + `GetLastByUserAsync` con `Task.WhenAll`
>
> **Test Coverage (10/10 passing)**:
> - Sin token → 401
> - Sin attempts → 200 con zeros y null lastAttemptAt
> - userId y displayName coinciden con el token/DB
> - TotalAttempts incluye todos (correctos e incorrectos)
> - CorrectAttempts solo cuenta los correctos
> - AccuracyRate = correctAttempts / totalAttempts (0.5 en test)
> - AllCorrect → accuracyRate = 1.0
> - Placeholder streak = 0
> - Placeholder ELO = 1000
> - lastAttemptAt refleja el attempt más reciente
>
> **Total Test Count**: 146/146 (30 Domain + 39 Infrastructure + 77 API)
> - Domain: User + Attempt + Challenge logic (unchanged: 30/30)
> - Infrastructure: DbContext + 3 repositories (unchanged: 39/39)
> - API: GET /challenges (13) + GET /challenges/{id} (8) + POST /attempt (26) + POST /auth/register (13) + POST /auth/login (11) + JWT middleware (9) + GET /users/me/stats (10) = 77/77
>
> **Code Changes**:
> - Created: `specs/api/get-user-stats.spec.md`
> - Created: `src/DevBrain.Api/DTOs/UserStatsResponseDto.cs`
> - Created: `src/DevBrain.Api/Endpoints/UserEndpoints.cs`
> - Created: `tests/DevBrain.Api.Tests/GetUserStatsTests.cs` (10 test methods)
> - Updated: `Program.cs` (agregado `app.MapUserEndpoints()`)
>
> **API Endpoints Summary**:
> - ✅ `GET /api/v1/challenges` — list with pagination + filtering (public)
> - ✅ `GET /api/v1/challenges/{id}` — single challenge detail (public)
> - ✅ `POST /api/v1/challenges/{id}/attempt` — submit answer **(requires JWT)**
> - ✅ `POST /api/v1/auth/register` — user registration + password hashing
> - ✅ `POST /api/v1/auth/login` — JWT token generation (24h expiration)
> - ✅ `GET /api/v1/users/me/stats` — user stats **(requires JWT)**
>
> **Next Step**:
> - `streak.spec.md` (Fase F) — requiere instalar y configurar Redis primero
> - `attempt-service.spec.md` (Fase D) — orquestar: guardar attempt + recalcular ELO + actualizar streak (depende de streak)

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
| Auth | JWT propio (HS256, 24h expiration) |
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
- [x] `attempt.spec.md` — actualizado con `UserId` (Guid del usuario) — 9 tests en verde
- [x] `user.spec.md` — entidad User básica (Guid Id, displayName, email, password hash) — 11 tests en verde
- [x] `ichallenge-repository.spec.md` — interfaz de persistencia de challenges (en Domain, sin EF)
- [x] `iattempt-repository.spec.md` — interfaz de persistencia de attempts (en Domain, sin EF)
- [x] `iuser-repository.spec.md` — interfaz de persistencia de usuarios (en Domain, sin EF)

### Fase B — Infraestructura
- [x] `devbrain-dbcontext.spec.md` — DbContext EF Core (tablas, configuraciones, migraciones, seed data)
- [x] `ef-challenge-repository.spec.md` — implementación EF de IChallengeRepository
- [x] `ef-attempt-repository.spec.md` — implementación EF de IAttemptRepository
- [x] `ef-user-repository.spec.md` — implementación EF de IUserRepository (AddAsync, GetByEmailAsync, GetByIdAsync) — sin test file dedicado, cubierto por API tests
- [ ] `seed-challenges.spec.md` — datos iniciales para poder probar el MVP (al menos 10 challenges)

### Fase C — Auth
- [x] `post-auth-login.spec.md` — POST /auth/login — email + password → JWT propio (11 tests, HS256, 24h expiration)
- [x] `jwt-middleware.spec.md` — JWT Bearer middleware + `.RequireAuthorization()` en POST /attempt (9 tests en verde)

### Fase D — Servicios de aplicación
- [ ] `attempt-service.spec.md` — orquesta: guardar attempt + actualizar streak + recalcular ELO

### Fase E — API endpoints
- [x] `get-challenges.spec.md` — GET /challenges — lista paginada con filtros por categoría y dificultad (13 tests en verde)
- [x] `get-challenge.spec.md` — GET /challenges/{id} — detalle de un challenge (8 tests en verde)
- [ ] `post-attempt.spec.md` — POST /challenges/{id}/attempt — enviar respuesta, devolver resultado + nuevo ELO
- [x] `get-user-stats.spec.md` — GET /users/me/stats — totalAttempts, correctAttempts, accuracyRate, streak/ELO placeholders (10 tests)

### Fase F — Gamificación
- [ ] `streak.spec.md` — regla de streak diario (Redis, se rompe si no hay attempt en 24h)
- [x] `elo-rating.spec.md` — cálculo de rating ELO global tras cada attempt (12 tests, fórmula ELO adaptada con time modifier)

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
- [x] Conectar PostgreSQL con EF Core — migrations aplicadas, schema creado en port 5433

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
