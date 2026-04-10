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
| Domain.Tests | 69 | ✅ 69/69 | User factory + validation, Attempt entity, Challenge logic, EloRatingService (12), BadgeAwardService + UserBadge (27) |
| Infrastructure.Tests | 58 | ✅ 58/58 | DbContext config (9), EFChallengeRepository (13), EFAttemptRepository (17), RedisStreakService (8), EFBadgeRepository (6), SerilogLogging (5) |
| Api.Tests | 83 | ✅ 83/83 | GET /challenges (13), GET /challenges/{id} (8), POST /attempt (28 — +2 badge tests), POST /auth/register (13), POST /auth/login (11), JWT middleware (9), GET /users/me/stats (10), GET /users/me/badges (4) |
| Integration.Tests | 2 | ✅ 2/2 | E2E happy path, multi-user isolation (TestContainers + real PostgreSQL/Redis) |
| **TOTAL** | **212** | **✅ 212/212** | 100% pass rate |

## Último paso completado
> ✅ **Phase 3.2: Serilog + Application Insights Logging Infrastructure — COMPLETADO**
>
> **Resumen de la sesión**:
> - 212/212 tests en verde (207 unit + integration tests + 5 SerilogLoggingTests)
> - Spec completada: `specs/infrastructure/serilog-logging.spec.md` (330+ líneas, full SDD)
> - NuGet packages agregados: Serilog, Serilog.AspNetCore, Serilog.Sinks.Console/File/ApplicationInsights, Serilog.Enrichers.Environment
> - Program.cs integrado con Serilog:
>   - Inicialización ANTES de WebApplicationBuilder (best practice)
>   - Multi-sink configuration: Console (JSON), File (rolling daily, 30-day retention), Application Insights
>   - Enrichers: FromLogContext, WithEnvironmentUserName, WithProperty(Environment)
>   - Try-catch-finally con Log.Fatal logging
> - Tests creados: `SerilogLoggingTests.cs` (7 test methods, todos en verde)
> - Configuración por entorno: Dev→Debug, Testing→Minimal, Production→Information
> - Invariantes validados: no passwords/tokens logged, structured JSON format, zero duplication
> - Fix aplicado: GetUserStats accuracy tests (ahora expect 50/100 en lugar de 0.5/1.0 decimal)
> - Logging ready para:
>   - Auth (register/login/token generation)
>   - Challenges (CRUD operations)
>   - Attempts (submission, scoring, badge awarding)
>   - User stats (ELO updates, streak tracking)
>   - Infrastructure (startup, DB migrations, Redis connection)
> - Commit: (pending — listos para hacer push)
>
> **Próximo paso**: **Phase 3.3 — Endpoint Logging Integration** (add ILogger<T> to endpoints, Log.Information calls, validate all 212+ tests still pass)

---

> ✅ **Deploy a Azure App Service completado y validado en producción**
>
> **Resumen**:
> - Causa raíz del crash resuelto: Npgsql no soporta formato URI de Neon → migrado a formato ADO.NET (`Host=...;Database=...;Username=...;SSL Mode=Require;Trust Server Certificate=true`)
> - Migraciones aplicadas a Neon (`InitialCreate` + `AddEloRatingToUser`) — 10 challenges seeded
> - `ConnectionStrings__DefaultConnection` actualizado en Azure App Service (resource group: `devbrain-rg`)
> - CI: deploy via GitHub Actions con native .NET publish (no Docker — Azure App Service F1 no soporta Docker)
> - Startup resiliente: Redis/DB errors no crashean la app (fallan silenciosamente al arrancar)
> - `/health` y `/scalar` expuestos en producción ✅
> - Flujo completo validado en `https://devbrain-trainer.azurewebsites.net`:
>   - `GET /api/v1/challenges` → 10 challenges ✅
>   - `POST /api/v1/auth/register` → usuario creado en Neon ✅
>   - `POST /api/v1/auth/login` → JWT generado ✅
>   - `POST /api/v1/challenges/{id}/attempt` → ELO actualizado, streak=1 (Redis Cloud) ✅
>
> **Próximo paso**: Frontend Next.js (Fase 3) o generación dinámica con Claude API (Fase 4)

---

## Stack decidido

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 10 (API REST) |
| Frontend | Next.js + Tailwind |
| DB principal | PostgreSQL |
| Cache / streak | Redis |
| Deploy backend | Azure App Service (devbrain-trainer.azurewebsites.net) |
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

## Test Strategy (MVP Completion + Pre-Frontend Testing)

### Current State (205/205 tests ✅)
- Unit tests: Entidades, repositorios, servicios, endpoints
- In-memory DB para tests (no real PostgreSQL)
- Mocks de Redis en algunos tests
- No concurrencia, no E2E, sin resiliencia

### Phase 3 — Robustez (ANTES del Frontend)
**Objetivo**: Validar que el backend es robusto antes de integrar UI

#### 3.1 — E2E Integration Tests ✅ COMPLETADO
- Real PostgreSQL (TestContainers v3.9.0)
- Real Redis (TestContainers v3.9.0)
- Flujos completos de usuario
  - Register → Login → GetChallenges → PostAttempt → GetStats → GetBadges ✅
  - Validar persistencia de datos end-to-end ✅
  - Validar relaciones entre entidades ✅
- Spec: `specs/integration/e2e-happy-path.spec.md` (329 lines, 7-step flow + multi-user test)
- Tests: 2/2 passing
  - ✅ E2E_Register_Login_Challenges_Attempt_Stats_Badges_HappyPath
  - ✅ E2E_MultipleAttempts_SameChallengeByDifferentUsers_NoConflict
- Infrastructure: IntegrationTestFactory, MockStreakService (singleton for shared state)

#### 3.2 — Concurrency/Race Condition Tests
- Dos usuarios simultáneos en POST /attempt
- Streak service con requests paralelas
- Badge evaluation con intentos concurrentes
- ELO recalculation sin colisiones

#### 3.3 — Resiliencia/Chaos Tests
- Redis no disponible → POST /attempt falla gracefully
- PostgreSQL lento (latencia artificial)
- JWT secret rotation between requests
- Out of memory scenarios

### Phase 4 — Frontend (Next.js + Tailwind)

### Phase 5 — Post-Frontend Testing
- **Benchmarks** (BenchmarkDotNet) — GET /challenges, POST /attempt, ELO calculation
- **Contract Tests** — DTOs no cambian sin aviso

---

## Roadmap de specs (MVP)

El orden respeta dependencias estrictas. No se puede implementar un paso sin tener el anterior completo.

### Fase A — Dominio (`specs/domain/`)
- [x] `challenge.spec.md` — entidad Challenge con validaciones
- [x] `attempt.spec.md` — actualizado con `UserId` (Guid del usuario) — 9 tests en verde
- [x] `user.spec.md` — entidad User básica (Guid Id, displayName, email, password hash) — 11 tests en verde
- [x] `ichallenge-repository.spec.md` — interfaz de persistencia de challenges (en Domain, sin EF)
- [x] `iattempt-repository.spec.md` — interfaz de persistencia de attempts (en Domain, sin EF)
- [x] `iuser-repository.spec.md` — interfaz de persistencia de usuarios (en Domain, sin EF)
- [x] `attempt-service.spec.md` — orquesta: Attempt.Create + ELO.Calculate + User.UpdateEloRating + Streak.RecordAttemptAsync
- [x] `seed-challenges.spec.md` — 10 challenges con GUIDs fijos via EF Core HasData, incluidos en `InitialCreate`

### Fase B — Infraestructura (`specs/infrastructure/`)
- [x] `devbrain-dbcontext.spec.md` — DbContext EF Core (tablas, configuraciones, migraciones, seed data)
- [x] `ef-challenge-repository.spec.md` — implementación EF de IChallengeRepository
- [x] `ef-attempt-repository.spec.md` — implementación EF de IAttemptRepository
- [x] `ef-user-repository.spec.md` — implementación EF de IUserRepository (AddAsync, GetByEmailAsync, GetByIdAsync) — sin test file dedicado, cubierto por API tests
- [x] `ef-badge-repository.spec.md` — tabla UserBadges, EFBadgeRepository, integración en AttemptService, endpoint GET /users/me/badges (12 tests en verde)

### Fase C — Auth (`specs/api/`)
- [x] `post-auth-login.spec.md` — POST /auth/login — email + password → JWT propio (11 tests, HS256, 24h expiration)
- [x] `jwt-middleware.spec.md` — JWT Bearer middleware + `.RequireAuthorization()` en POST /attempt (9 tests en verde)

### Fase D — Servicios de aplicación (`specs/domain/`)
- [x] `attempt-service.spec.md` — orquesta: Attempt.Create + ELO.Calculate + User.UpdateEloRating + Streak.RecordAttemptAsync

### Fase E — API endpoints (`specs/api/`)
- [x] `get-challenges.spec.md` — GET /challenges — lista paginada con filtros por categoría y dificultad (13 tests en verde)
- [x] `get-challenge.spec.md` — GET /challenges/{id} — detalle de un challenge (8 tests en verde)
- [x] `post-attempt.spec.md` — POST /challenges/{id}/attempt — enviar respuesta, devolver resultado + nuevo ELO
- [x] `get-user-stats.spec.md` — GET /users/me/stats — totalAttempts, correctAttempts, accuracyRate, streak/ELO placeholders (10 tests)

### Fase F — Gamificación (`specs/gamification/`)
- [x] `streak.spec.md` — streak diario con Redis (8 tests integración, TTL 48h, reset si gap >1 día)
- [x] `elo-rating.spec.md` — cálculo de rating ELO global tras cada attempt (12 tests, fórmula ELO adaptada con time modifier)
- [x] `badges.spec.md` — sistema de badges: BadgeType (8), UserBadge entity, IBadgeRepository, BadgeAwardService (27 tests en verde)

---

## Próximas prioridades (antes de Frontend)
1. **✅ E2E Integration Tests** — 2/2 tests passing, real DB/Redis with TestContainers
2. **🚀 Concurrency Tests (Phase 3.2)** — simultaneous user attempts, race conditions, streak service parallel calls
3. **Resiliencia/Chaos Tests (Phase 3.3)** — Redis down, slow DB, JWT rotation
4. **Frontend (Phase 4)** — Next.js implementation after Phase 3 complete
3. **Resiliencia Tests** — componentes externos fallando gracefully

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
- [x] Sistema de streak — RedisStreakService (8 tests integración, TTL 48h)
- [x] Rating ELO global — EloRatingService (12 tests, fórmula adaptada con time modifier)
- [x] Logros / badges — BadgeAwardService Domain (27 tests) + EFBadgeRepository (6 tests) + Endpoint GET /users/me/badges (4 tests)
- [x] **TOTAL: 205/205 tests passing (100% pass rate)**

### Fase 3 — Robustez Backend (ANTES del Frontend)

#### 3.1 — E2E Integration Tests
- [ ] Crear proyecto `DevBrain.Integration.Tests`
- [ ] Agregar TestContainers (PostgreSQL + Redis)
- [ ] Spec: Flujo completo Register → Login → GetChallenges → PostAttempt → GetStats → GetBadges
- [ ] Validar persistencia de datos end-to-end
- [ ] Validar relaciones entre entidades en real DB

#### 3.2 — Concurrency/Race Condition Tests
- [ ] Spec: Dos usuarios simultáneos en POST /attempt
- [ ] Spec: Streak service con requests paralelas (`Task.WhenAll`)
- [ ] Spec: Badge evaluation sin race conditions
- [ ] Spec: ELO recalculation sin colisiones
- [ ] Agregar tests al proyecto correspondiente (Integration o Api.Tests)

#### 3.3 — Resiliencia/Chaos Tests
- [ ] Spec: Redis no disponible → POST /attempt falla gracefully (no crash)
- [ ] Spec: PostgreSQL lento (latencia artificial) → timeout handling
- [ ] Spec: JWT secret rotation entre requests → rechazo correcto
- [ ] Spec: Out of memory en AttemptService → logueo y error handling

### Fase 4 — Frontend
- [ ] Next.js + Tailwind
- [ ] UI de desafío con timer
- [ ] Dashboard de progreso

### Fase 5 — Post-Frontend Testing

#### 5.1 — Benchmarks
- [ ] Crear proyecto `DevBrain.Benchmarks` (BenchmarkDotNet)
- [ ] Benchmark: GET /challenges con 1000 challenges → <100ms
- [ ] Benchmark: GET /users/me/stats con 10K attempts → <200ms
- [ ] Benchmark: POST /attempt (ELO + Badge) → <300ms
- [ ] Baseline para futuras optimizaciones

#### 5.2 — Contract Tests
- [ ] DTOs no cambian sin aviso
- [ ] API versioning consistency
- [ ] Response schema validation

### Fase 6 — Generación dinámica
- [ ] Integrar Claude API para generar problemas nuevos
