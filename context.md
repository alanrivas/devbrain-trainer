# DevBrain Trainer — Estado del Proyecto

## ⚠️ ADVERTENCIA CRÍTICA: NO ejecutar Stress Tests contra Azure

**NO EJECUTES unittest de stress, load testing, o pruebas de alta concurrencia** (>100 requests) contra Azure App Service.

- El plan **F1 Free** tiene cuota: 60 minutos CPU/mes
- Un stress test agota esto en **segundos**
- Cuando se agota → HTTP 403 "quota exceeded" hasta fin de mes
- **Los tests normales son seguros (5-99 requests concurrentes)**

**Alternativa**: Usar máquina local o cloud con plan de pago.

Ver `docs/DEVELOPMENT.md` sección "⚠️ IMPORTANTE: NO ejecutar Load/Stress Tests en Azure" para detalles completos.

---

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
| Infrastructure.Tests | 71 | ✅ 71/71 | DbContext config (9), EFChallengeRepository (13), EFAttemptRepository (17), RedisStreakService (8), EFBadgeRepository (6), SerilogLogging (5), LogLevelConfiguration (13) |
| Api.Tests | 99 | ✅ 99/99 | Phase 3.3 fix: ILoggerFactory, CustomWebApplicationFactory, LoginResponseDto + Phase 3.3.1: DynamicLogLevelConfiguration (6) |
| Integration.Tests | 10 | ✅ 10/10 | E2E happy path (2) + **Phase 3.4: Chaos/Resilience (8)** |
| **Current** | **249** | **✅ 249/249** | Domain (69) + Infrastructure (71) + Api.Tests (99) + Integration.Tests (10) |

## Último paso completado
> ✅ **Frontend Phase 4.1.1 COMPLETADO + Phase 4.2.1 Spec Created — 13 de Abril 2026**
>
> **Cambios realizados**:
> 
> **1. Agent créado: next-spec** 
> - Automatiza el ciclo completo: read context → write spec → spec-implement → update context → commit+push
> - Ubicación: `.github/agents/next-spec/AGENT.md`
> - Uso: `runSubagent agentName="next-spec"` para implementar siguiente spec
> - Workflows para Fase 4 (Frontend), Fase 5 (Testing), etc.
>
> **2. Primera Spec de Frontend Creada**:
> - **Phase 4.2.1 — Login Page** spec.md ✅
> - Ubicación: `specs/frontend/phase-4.2.1-login-page.spec.md`
> - Completamente especificado con:
>   - Comportamientos funcionales (rendering, validación, error handling)
>   - Contrato HTTP (/api/v1/auth/login — ya existe en backend ✅)
>   - Componentes a crear: LoginForm.tsx, page.tsx
>   - 5 categorías de tests (15+ casos de test)
>   - Integración con AuthContext + ApiClient
>   - Diseño responsive con Tailwind CSS
>   
> **3. Estructura de Frontend Specs**:
> - Carpeta creada: `specs/frontend/`
> - Patrón de naming: `phase-X.X.X-{feature-name}.spec.md`
> 
> **Próximo paso**: 
> - Ejecutar skill `spec-implement` para generar LoginForm.tsx + login/page.tsx + tests
> - O usar agente `next-spec` cuando esté integrado en sistema de agentes
> - Luego: Phase 4.2.2 (Register Page)

## 🟠 ESTADO DE DEPLOYMENTS — PARADO POR CUOTA

**Problema**: Plan F1 Free (Azure para estudiantes: Starter) consumió cuota mensual
- **Estado actual**: `QuotaExceeded` — App rechaza requests con HTTP 403
- **Duración**: Hasta fin de mes (próximo reset)
- **Recursos creados**: 
  - Frontend App: `devbrain-frontend.azurewebsites.net` (parado)
  - Backend API: `devbrain-trainer.azurewebsites.net` (parado)
  - Plan: `devbrain-plan` (F1 Free)
  - Resource Group: `devbrain-rg`

### ⏳ Próximas acciones (cuando la cuota se resetee — fin de mes):

1. **Verificar que cuota se reabre**:
   ```powershell
   az webapp show -g devbrain-rg -n devbrain-frontend --query "state"
   # Debería cambiar de "QuotaExceeded" a "Running"
   ```

2. **Probar frontend**:
   ```
   https://devbrain-frontend.azurewebsites.net
   ```

3. **Alternativa inmediata** (si no puedo esperar):
   - Migrar a **Static Web Apps** (~$0.20 USD/mes, sin limites de cuota)
   - Comando: Ver skill `/deploy-gh-pages` para Static Web Apps

### 📝 Credenciales para prueba:
- Email: `admin@devbrain.local`
- Password: `Admin123!`

### 🔗 URLs a recordar:
- Frontend: `https://devbrain-frontend.azurewebsites.net`
- Backend: `https://devbrain-trainer.azurewebsites.net/api/v1`
- Login endpoint: `POST https://devbrain-trainer.azurewebsites.net/api/v1/auth/login`
> - `.azure/ci-cd.yml` — Pipeline de CI/CD con Azure Pipelines ✅
> - `docs/AZURE-DEPLOYMENT.md` — Guía completa de deployment ✅
> 
> **Build Status**:
> - ✅ Frontend builds sin errores
> - ✅ Assets optimizados para producción
> - ✅ Next.js 16.2.3 listo para Azure
> 
> **Próximos Pasos**:
> 1. Ejecutar: `az login`
> 2. Seguir guía: `docs/AZURE-DEPLOYMENT.md`
> 3. Depurar con: `az webapp log tail`
> 4. Acceder en: https://devbrain-frontend.azurewebsites.net
> 
> **Configuración Azure**:
> - Resource Group: `devbrain-rg`
> - App Service Plan: `devbrain-plan`
> - Frontend App: `devbrain-frontend`
> - Backend App: `devbrain-trainer` (ya existe)
> - Región: `eastus` (recomendado)

---

## Último paso completado (anterior)
> ✅ **Session Restart + Full System Verification — 13 de Abril 2026**
>
> **Cambios realizados en esta sesión**:
> 
> **1. Docker Configuration**:
> - Cambio de puerto PostgreSQL: `5432:5432` → `5433:5432` ✅
> - Verificación de contenedores: PostgreSQL + Redis activos ✅
> 
> **2. Backend Improvements**:
> - Agregado logging de connection string en Program.cs ✅
> - Restricción de auto-migration a Production (exclude tests/local) ✅
> - Backend iniciado correctamente en puerto 5118 ✅
> 
> **3. System Verification Checklist** (✅ 100%):
> - ✅ PostgreSQL accesible (puerto 5433, user: admin, pass: admin)
> - ✅ Redis accesible (puerto 6380)
> - ✅ Schema completo (users, challenges, attempts, badges, streaks)
> - ✅ Seed data verificado (1 admin user + 5 challenges)
> - ✅ Backend process running (ASP.NET Core 10, Development env)
> - ✅ Health endpoint responding (/health)
> - ✅ Authentication working (admin@devbrain.local login successful)
> - ✅ Challenges endpoint returning paginated data
> - ✅ Structured logging active (Serilog + JSON format)
> - ✅ All 249 tests ready to run
> 
> **Admin Credentials Verified**:
> - Email: `admin@devbrain.local`
> - Password: `Admin123!`
> - ELO Rating: Initial (---)
> - Status: ✅ ACTIVE
> 
> **Infrastructure Status**:
> - Backend: http://localhost:5118 ✅
> - PostgreSQL: localhost:5433 ✅
> - Redis: localhost:6380 ✅
> 
> **Archivos Eliminados** (auxiliares de esta sesión):
> - SYSTEM_STATUS.md (reporte de verificación)
> - LOCAL-SETUP-DOCUMENTATION.md (documentación temporal)
> - Otros archivos auxiliares temporales
> 
> **Próximo paso**: Phase 4.3 — Challenge Pages (detail page + attempt form)

---

> ✅ **Phase 4.2 + Local DB Setup + System Verification — COMPLETADO**
>
> **Resumen Completo**:
> 
> **Frontend (Phase 4.2)**:
> - LoginForm y RegisterForm components ✅
> - `/login` y `/register` pages con auth flow ✅
> - Next.js 15 en http://localhost:3000 ✅
> 
> **Backend Infrastructure**:
> - ASP.NET Core 10 escuchando en http://localhost:5118 ✅
> - JWT authentication (HS256, 24h expiry) ✅
> - PostgreSQL en Docker (puerto 5433, base `devbrain_local`) ✅
> - Redis en Docker (puerto 6379, NoOp fallback) ✅
> 
> **Base de Datos Local**:
> - Usuario `devbrain` con contraseña `admin` (SUPERUSER) ✅
> - Tabla `users` con 1 admin seeded: `admin@devbrain.local` / `Admin123!` ✅
> - Tabla `challenges` con 5 challenges seeded (SQL, C#, Docker, Architecture) ✅
> - EF Core migrations aplicadas (InitialCreate, AddEloRatingToUser, AddUserBadgesTable) ✅
> 
> **Authentication Tested**:
> - Login endpoint: ✅ Returns JWT token
> - User profile: ✅ Correctly loaded
> - Password verification: ✅ PBKDF2 working
> 
> **Documentation**:
> - `LOCAL-SETUP-DOCUMENTATION.md` → Setup completo + troubleshooting ✅
> - `QUICK-COMMANDS.md` → Comandos rápidos para desarrollo ✅
> - `SYSTEM-STATUS-APRIL-13-2026.md` → Reporte de estado verificado ✅
> 
> **Tests**: 249/249 ✅ (sin cambios, todos pasando)
> 
> **Configuración Multi-entorno**:
> - `appsettings.Development.json` (Docker, 5433) ✅
> - `appsettings.Local.json` (Sistema local, 5432) ✅
> - `appsettings.Docker.json` (Docker, 5433) ✅
> - `appsettings.Production.json` (Neon) ✅
>
> **Próximo paso**: Phase 4.3 — Challenge Pages (detail page + attempt form)
>   - Redis: localhost:6379 (Docker)
> - **Documentación creada**:
>   - `docs/TESTING.md` — Guía completa de testing con flujos y troubleshooting
>   - `docs/TEST-USERS.md` — Usuarios precreados y credenciales
>   - `docs/LOCAL-SETUP.md` — Estado actual del stack + acceso rápido
> - **Tests**: 249/249 ✅ (Domain 69 + Infrastructure 71 + Api 99 + Integration 10)
> - **Infrastructure**: NoOpStreakService implementado (fallback cuando Redis no disponible)
>
> **Próximo paso**: **Phase 4.3 — Challenge Pages** (detail page + attempt form)

---

> ✅ **Phase 3.3.1: Dynamic Log Level Configuration — COMPLETADO — 241/241 tests ✅**
>
> **Objetivo completado**: Permitir cambiar el nivel de log (Debug/Info/Warning/Error/Fatal) sin redeploy usando variable de entorno `SERILOG__MINIMUMLEVEL`.
>
> **Implementación**:
> - Spec: `specs/infrastructure/dynamic-log-level.spec.md` (completa con 10 escenarios de test)
> - Unit tests: `tests/DevBrain.Infrastructure.Tests/LogLevelConfigurationTests.cs` (13 tests)
>   - Parseo válido para todos los niveles: Debug, Information, Warning, Error, Fatal, Verbose
>   - Case-insensitive: "debug", "DEBUG", "DeBuG" → todos parsan a LogEventLevel.Debug
>   - Fallback a Information: valores inválidos, string vacío, null
> - Integration tests: `tests/DevBrain.Api.Tests/DynamicLogLevelConfigurationTests.cs` (6 tests)
>   - App arranca correctamente con env var `SERILOG__MINIMUMLEVEL=Debug|Information|Error|etc`
>   - Fallback a default Information si valor es inválido
>   - Case-insensitive parsing
> - Implementación en `Program.cs`:
>   ```csharp
>   var minLevelStr = Environment.GetEnvironmentVariable("SERILOG__MINIMUMLEVEL") ?? "Information";
>   var minLevel = Enum.TryParse<LogEventLevel>(minLevelStr, ignoreCase: true, out var parsedLevel)
>       ? parsedLevel
>       : LogEventLevel.Information;
>   var logger = new LoggerConfiguration()
>       .MinimumLevel.Is(minLevel)  // ← Dinámico según env var
>       ...
>   ```
> - Resultado: **241/241 tests passing** (222 anteriores + 13 unitarios + 6 integración)
> 
> **Verificación en producción**:
> - Deploy cambios a Azure App Service via GitHub Actions
> - Vía Azure Portal: Configuration → Add app setting: `SERILOG__MINIMUMLEVEL=Debug`
> - Restart app service → logs cambian a Debug level en Application Insights
>
> **Próximo paso**: **Phase 3.4 — Resiliencia/Chaos Tests** O **Phase 4: Frontend Next.js**

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

#### 3.3 — Resiliencia/Chaos Tests ✅ COMPLETADO
- ✅ Redis no disponible → POST /attempt falla gracefully
- ✅ PostgreSQL lento (latencia artificial)
- ✅ JWT validation y handling
- ✅ Out of memory scenarios
- Tests: 8/8 passing en `tests/DevBrain.Integration.Tests/ChaosResilienceTests.cs`

### Phase 4 — Frontend (Next.js + Tailwind) — ✅ INICIADO

#### 4.1 — Setup Next.js 15 ✅ COMPLETADO
- [x] Proyecto creado en `frontend/` con `npm create next-app@latest`
- [x] TypeScript habilitado con `tsconfig.json`
- [x] Tailwind CSS configurado
- [x] App Router modo
- [x] ESLint configurado
- [x] `src/` directory structure

#### 4.1.1 — Configuración inicial (THIS SESSION) ✅ COMPLETADO
- [x] **Environment variables**:
  - `.env.local`: `NEXT_PUBLIC_API_URL=http://localhost:5118/api/v1`
  - Producción (futura): `NEXT_PUBLIC_API_URL=https://devbrain-trainer.azurewebsites.net/api/v1`

- [x] **Dependencias instaladas**:
  - `axios` (^1.15.0) — HTTP client con JWT interceptors
  - `zustand` (^5.0.12) — state management (preparado para futuro uso)

- [x] **Servicios creados** (`src/lib/`):
  - `api.ts` — Axios client con interceptores para JWT auth + auto-logout en 401
  - `auth.ts` — Utilidades de JWT: `getToken()`, `setToken()`, `getUser()`, `setUser()`, `login()`, `logout()`, `isAuthenticated()`

- [x] **Context & Providers** (`src/components/`):
  - `AuthContext.tsx` — React Context para autenticación global + hook `useAuth()`
  - `Header.tsx` — Componente header con navigation, condicional Login/Register vs Logout

- [x] **Layout & Pages**:
  - `src/app/layout.tsx` — RootLayout con AuthProvider + Header integrados
  - `src/app/page.tsx` — Home page responsiva con CTA (Get Started / Sign In) cuando no autenticado, o (Start Challenges / View Stats) cuando autenticado

- [x] **Git setup**:
  - Commit: `feat: Next.js 15 frontend scaffold (Phase 4)`
  - Pushed a `main` branch

**Estructura creada**:
```
frontend/
├── src/
│   ├── app/
│   │   ├── layout.tsx (con AuthProvider + Header)
│   │   ├── page.tsx (home placeholder)
│   │   └── globals.css
│   ├── components/
│   │   ├── AuthContext.tsx
│   │   └── Header.tsx
│   └── lib/
│       ├── api.ts (axios client)
│       └── auth.ts (JWT helpers)
├── .env.local
├── package.json (axios, zustand, next, react, tailwind)
├── next.config.ts
├── tailwind.config.ts
├── tsconfig.json
└── .gitignore
```

**Funcionalidad lista para desarrollo**:
- `npm run dev` en `frontend/` → servidor Next.js en puerto 3000
- Conexión preparada a backend ASP.NET en `http://localhost:5118/api/v1`
- JWT handling en localStorage + auto-refresh headers
- Context global de autenticación disponible

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

## Próximas prioridades (roadmap futuro)

### ✅ COMPLETADAS (Phase 3)
1. **✅ E2E Integration Tests** — 2/2 tests passing, real DB/Redis with TestContainers
2. **✅ Resiliencia/Logging - Dynamic Log Level (Phase 3.3.1)** — SERILOG__MINIMUMLEVEL env var
3. **✅ Chaos/Resilience Tests (Phase 3.4)** — JWT validation, graceful error handling, no crashes

### 📋 PENDING (Dejar para sesiones futuras si es necesario)
4. **🚀 Concurrency Tests (Phase 3.2)** — simultaneous user attempts, race conditions, streak service parallel calls
5. **🚀 Post-MVP Optimizations (Phase 3.5)** — Benchmarks (BenchmarkDotNet), Contract Tests (DTOs stability)

### 🔄 IN PROGRESS
6. **✅ Frontend Phase 4.1** — Next.js scaffold con auth context, API client, Header component
7. **▶️ Frontend Phase 4.2** — Pages: Login, Register, Challenges list, Challenge detail, User stats, Badges
   - **4.2.1** — Login Page (spec created, ready for spec-implement) 🚀
   - **4.2.2** — Register Page (pending)
   - **4.2.3** — Challenges List (pending)
   - **4.2.4** — Challenge Detail + Attempt Form (pending)
   - **4.2.5** — User Stats Dashboard (pending)
   - **4.2.6** — Badges Page (pending)

### 🎯 PRÓXIMAS FASES
8. **▶️ Frontend Phase 4.3** — Integration testing (Cypress / Playwright)
9. **▶️ Frontend Phase 4.4** — Deploy a GitHub Pages / Vercel
10. **▶️ Phase 5** — Post-Frontend Testing (Benchmarks, Contract Tests)


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

#### 3.1 — E2E Integration Tests ✅ COMPLETADO
- [x] Crear proyecto `DevBrain.Integration.Tests`
- [x] Agregar TestContainers (PostgreSQL + Redis)
- [x] Spec: Flujo completo Register → Login → GetChallenges → PostAttempt → GetStats → GetBadges
- [x] Validar persistencia de datos end-to-end
- [x] Validar relaciones entre entidades en real DB

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

---

## Análisis de Workflow: Necesidad de Nuevos Skills/Agentes

### Patrón identificado en esta sesión (Phase 3.3)

**El ciclo SDD+TDD funciona excelente para:**
- ✅ Especificar e implementar features nuevas
- ✅ Garantizar tests en verde en contexto aislado
- ✅ Documentar decisiones de diseño

**PERO identificamos un gap:**
- ❌ No hay proceso para debuggear tests fallidos post-implementación
- ❌ Cambios bien intencionados (Dynamic Log Level Config) rompieron 93 tests
- ❌ Fue necesario: investigación ad-hoc, múltiples revertes, diagnóstico manual

### Síntomas del gap
1. **EndpointLoggingTests failing (9 tests → HTTP 500)**
   - Causa: Minimal API parameter ordering (ILogger debe venir después de DI services, antes de body params)
   - Impacto: 93 API tests no pueden pasar hasta fijar esto
   - Tiempo investigación: ~2 horas

2. **Dynamic Log Level Config intentos**
   - Cambio aparentemente seguro en Program.cs
   - Resultado: rompe inicialización de CustomWebApplicationFactory
   - Necesitó: 3 revertes, testing iterativo, finalmente abandoned (por time constraints)

### Solución Propuesta: Nuevo Skill `debug-test-failures`

**Propósito**: Diagnosticar y documentar test failures sistemáticamente
- Ejecutar tests con diferentes niveles de verbosidad
- Capturar stack traces completos
- Aislar causa raíz (compilation, routing, DI, async, schema, etc.)
- Comparar con último commit exitoso
- Documentar hallazgos en `DEBUG_NOTES.md`
- Proponer: fijar, revertir, o documentar como tarea pendiente

**Estructura del skill**:
```
.github/skills/debug-test-failures/
├── SKILL.md                    # Instrucciones step-by-step
├── test-diagnostics.ps1        # Helper para correr tests + captura de output
└── common-patterns.md          # Biblioteca de errores comunes + soluciones
```

**Casos de uso inmediatos**:
1. [PENDIENTE] Phase 3.3 EndpointLoggingTests debugging (93 failing tests)
2. [PENDIENTE] Dynamic Log Level Config investigation (si se retoma)
3. [FUTURO] Cualquier regresión post-merge en phases siguientes

### Opcional: Agente `test-debugger`

Para investigaciones más complejas que requieran iteración autónoma y decisiones heurísticas.

**Diferencia**: Un skill = procedimiento determinístico; un agente = investigación iterativa

**No urgente**, pero sería útil para:
- Bifurcación de commits (`git bisect`) en caso de regresiones
- Análisis de cobertura vs. failure patterns
- Recomendaciones automáticas (revertir vs. fijar)

---

## Recomendación para próxima sesión

1. ✅ Phase 3.3 fix completado — 222/222 tests en verde
2. ▶️ **Siguiente**: Phase 3.3.1 — Dynamic Log Level Configuration (`SERILOG__MINIMUMLEVEL` desde env var)

---

## Gestión de Archivos Auxiliares (Cleanup Strategy)

### Problema identificado
Durante debugging y testing, se generan archivos auxiliares que:
- NO son parte del proyecto (son artifacts de ejecución)
- Contaminan el workspace
- Pueden causar confusión/distracciones
- NO deberían commitearse a GitHub

**Ejemplos**: `api_test_error.txt`, `tests_output.txt`, `debug_notes.txt`

### Solución Implementada

#### 1. Actualizado `.gitignore` ✅
Agregadas líneas para excluir automáticamente:
```
# Debug/Testing artifacts (created during troubleshooting, not part of source)
*_test_error.txt
*_test_output.txt
api_test_error.txt
tests_output.txt
debug_notes.txt
*.dump
*.trace
```

#### 2. Cleanup Manual Realizado ✅
- Borrados: `api_test_error.txt`, `tests_output.txt`
- Working directory limpio
- `git status` = "nothing to commit"

#### 3. Integración Futura: Skill `debug-test-failures` ⏳

Cuando se implemente el nuevo skill, debería:

**Al inicio de diagnostics:**
```powershell
# Cleanup previos artifacts
Remove-Item -Force @(
    "*_test_error.txt",
    "*_test_output.txt", 
    "debug_notes.txt"
) -ErrorAction SilentlyContinue
```

**Durante ejecución:**
- Guardar test output con timestamp (ej: `test_output_20260410-1430.txt`)
- Permisos para redirecciones temporales

**Al finalizar:**
- Generar `DEBUG_NOTES.md` final con hallazgos
- Limpiar archivos .txt temporales
- Deixar proyecto limpio para próximo paso

**Beneficio**: Workspace siempre limpio, documentación centralizada en `DEBUG_NOTES.md`

### Estado Actual
✅ `.gitignore` actualizado (artifacts serán ignorados)
✅ Archivos auxiliares borrados (workspace limpio)
⏳ Integración en skill (cuando se implemente `debug-test-failures`)


## 🚀 PRODUCTION DEPLOYMENT - COMPLETED ✅

**Deployment Date**: April 13, 2026  
**Status**: ✅ LIVE

### Azure Resources
- **Frontend Web App**: https://devbrain-frontend.azurewebsites.net
- **Backend API**: https://devbrain-trainer.azurewebsites.net/api/v1
- **Resource Group**: devbrain-rg (eastus)
- **App Service Plan**: devbrain-plan (Free tier)
- **Runtime**: Node.js 20 LTS

### Environment Configuration
- Frontend builds with Next.js 16.2.3
- Backend API: ASP.NET Core 10
- Database: PostgreSQL 16 (Neon)
- Authentication: JWT (24h expiry)
- CORS: Configured for production

### Test Access
- Email: admin@devbrain.local
- Password: Admin123!
- URL: https://devbrain-frontend.azurewebsites.net

### Monitoring
- Logs: \z webapp log tail -g devbrain-rg -n devbrain-frontend\
- Dashboard: Azure Portal → devbrain-frontend
- Alerts: Configure in Azure Portal if needed

### Deployment Method
- ZIP deployment from local build
- Frontend: .next directory + Next.js runtime
- Node startup: \
pm start\ configured

### Next Steps (Optional)
1. Configure custom domain
2. Set up Azure Monitor alerts
3. Implement CI/CD via GitHub Actions
4. Scale up from Free tier if needed


## 🚀 PRODUCTION DEPLOYMENT - COMPLETED ✅

**Deployment Date**: April 13, 2026  
**Status**: ✅ LIVE

### Azure Resources
- **Frontend Web App**: https://devbrain-frontend.azurewebsites.net
- **Backend API**: https://devbrain-trainer.azurewebsites.net/api/v1
- **Resource Group**: devbrain-rg (eastus)
- **App Service Plan**: devbrain-plan (Free tier)
- **Runtime**: Node.js 20 LTS

### Environment Configuration
- Frontend builds with Next.js 16.2.3
- Backend API: ASP.NET Core 10
- Database: PostgreSQL 16 (Neon)
- Authentication: JWT (24h expiry)
- CORS: Configured for production

### Test Access
- Email: admin@devbrain.local
- Password: Admin123!
- URL: https://devbrain-frontend.azurewebsites.net

### Monitoring
- Logs: \z webapp log tail -g devbrain-rg -n devbrain-frontend\
- Dashboard: Azure Portal → devbrain-frontend
- Alerts: Configure in Azure Portal if needed

### Deployment Method
- ZIP deployment from local build
- Frontend: .next directory + Next.js runtime
- Node startup: \
pm start\ configured

### Next Steps (Optional)
1. Configure custom domain
2. Set up Azure Monitor alerts
3. Implement CI/CD via GitHub Actions
4. Scale up from Free tier if needed


## 🚀 PRODUCTION DEPLOYMENT - COMPLETED ✅

**Deployment Date**: April 13, 2026  
**Status**: ✅ LIVE

### Azure Resources
- **Frontend Web App**: https://devbrain-frontend.azurewebsites.net
- **Backend API**: https://devbrain-trainer.azurewebsites.net/api/v1
- **Resource Group**: devbrain-rg (eastus)
- **App Service Plan**: devbrain-plan (Free tier)
- **Runtime**: Node.js 20 LTS

### Environment Configuration
- Frontend builds with Next.js 16.2.3
- Backend API: ASP.NET Core 10
- Database: PostgreSQL 16 (Neon)
- Authentication: JWT (24h expiry)
- CORS: Configured for production

### Test Access
- Email: admin@devbrain.local
- Password: Admin123!
- URL: https://devbrain-frontend.azurewebsites.net

### Monitoring
- Logs: \z webapp log tail -g devbrain-rg -n devbrain-frontend\
- Dashboard: Azure Portal → devbrain-frontend
- Alerts: Configure in Azure Portal if needed

### Deployment Method
- ZIP deployment from local build
- Frontend: .next directory + Next.js runtime
- Node startup: \
pm start\ configured

### Next Steps (Optional)
1. Configure custom domain
2. Set up Azure Monitor alerts
3. Implement CI/CD via GitHub Actions
4. Scale up from Free tier if needed
