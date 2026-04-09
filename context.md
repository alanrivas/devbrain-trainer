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
- [x] Swagger UI con Scalar (`/scalar/v1`) — spec OpenAPI en `/openapi/v1.json`
- [x] Docker — `Dockerfile` multi-stage + `docker-compose.yml` (API + PostgreSQL 17 + Redis 7)
- [x] Colección Postman — `postman/devbrain-trainer.postman_collection.json` con todos los endpoints del MVP y ejemplos 200/400/401/404
- [x] Skills `spec-implement` y `write-spec` actualizados — incluyen paso de actualización de colección Postman al terminar specs de API
- [ ] Endpoint GET /challenges
- [ ] Endpoint POST /challenges/:id/attempt
- [ ] Conectar PostgreSQL con EF Core

## Último paso completado
> Infraestructura de desarrollo completa: Scalar (Swagger UI), Docker (Dockerfile + docker-compose con PostgreSQL 17 + Redis 7), colección Postman con todos los endpoints MVP y ejemplos de 200/400/401/404. Skills `spec-implement` y `write-spec` actualizados para incluir actualización de Postman al terminar specs de API. context.md sincronizado con el estado real del proyecto.  
> Total: 30 tests en verde (10 Challenge + 9 Attempt + 11 User).  
> Próximo paso: `iattempt-repository.spec.md`.

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
- [ ] `iattempt-repository.spec.md` — interfaz de persistencia de attempts (en Domain, sin EF)

### Fase B — Infraestructura
- [ ] `devbrain-dbcontext.spec.md` — DbContext EF Core (tablas, configuraciones, migraciones)
- [ ] `ef-challenge-repository.spec.md` — implementación EF de IChallengeRepository
- [ ] `ef-attempt-repository.spec.md` — implementación EF de IAttemptRepository
- [ ] `seed-challenges.spec.md` — datos iniciales para poder probar el MVP (al menos 10 challenges)

### Fase C — Auth
- [ ] `supabase-auth.spec.md` — validación de JWT Supabase en ASP.NET Core, extracción de userId

### Fase D — Servicios de aplicación
- [ ] `attempt-service.spec.md` — orquesta: guardar attempt + actualizar streak + recalcular ELO

### Fase E — API endpoints
- [ ] `get-challenges.spec.md` — GET /challenges — lista paginada con filtros por categoría y dificultad
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
- [ ] Endpoint GET /challenges
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
