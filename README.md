# DevBrain Trainer

App de entrenamiento cognitivo gamificada para desarrolladores. Mejora tu lógica, memoria y razonamiento con problemas del mundo tech real.

**Estado**: Backend API completo — **212 tests (100% passing)** ✅ | Phase 3.2: Serilog logging infrastructure | Frontend próximo

**Docs**: 
- [`context.md`](./context.md) — Estado del proyecto y roadmap detallado
- [`STACK.md`](./STACK.md) — Tech stack completo con setup guides
- [`docs/AGENTS-SKILLS-REFERENCE.md`](./docs/AGENTS-SKILLS-REFERENCE.md) — **Todos los agentes y skills disponibles con ejemplos**
- [`docs/DEVELOPMENT.md`](./docs/DEVELOPMENT.md) — Metodología SDD+TDD
- [`docs/API-ENDPOINTS.md`](./docs/API-ENDPOINTS.md) — Referencia completa de endpoints
- [`CLAUDE.md`](./CLAUDE.md) — Instrucciones para Claude Code
- [`.github/copilot-instructions.md`](./.github/copilot-instructions.md) — Instrucciones para GitHub Copilot

---

## Quick Start

### Prerequisites
- **.NET 10** (`dotnet --version`)
- **PowerShell** (recomendado en Windows)
- **PostgreSQL 16+** (para deploy, tests usan in-memory)

### Run Backend
```bash
cd c:\dev\devbrain-trainer

# Build
dotnet build

# Run tests
dotnet test

# Run API (http://localhost:5000)
dotnet run --project src/DevBrain.Api/
```

### Run Tests
```bash
# All tests
dotnet test

# Domain tests only
dotnet test tests/DevBrain.Domain.Tests/

# API tests only
dotnet test tests/DevBrain.Api.Tests/

# Infrastructure tests only
dotnet test tests/DevBrain.Infrastructure.Tests/
```

**Test Coverage**: **212/212 tests passing** (100% green) ✅
- Domain.Tests: 69 tests
- Infrastructure.Tests: 58 tests (+5 Serilog logging tests)
- Api.Tests: 83 tests
- Integration.Tests: 2 tests (E2E with TestContainers)

---

## Project Structure

```
devbrain-trainer/
├── src/
│   ├── DevBrain.Api/                    ← ASP.NET Core 10 Web API
│   │   ├── Endpoints/                   ← Route handlers (Challenge, Attempt endpoints)
│   │   ├── DTOs/                        ← Request/Response DTOs
│   │   ├── Mapping/                     ← Entity-to-DTO mappers
│   │   └── Program.cs                   ← App configuration
│   ├── DevBrain.Domain/                 ← Business logic & entities
│   │   ├── Entities/                    ← Challenge, Attempt, User
│   │   ├── Interfaces/                  ← IChallengeRepository, IAttemptRepository
│   │   ├── Enums/                       ← ChallengeCategory, Difficulty
│   │   └── Exceptions/                  ← DomainException
│   └── DevBrain.Infrastructure/         ← EF Core & persistence
│       ├── Persistence/                 ← DevBrainDbContext
│       ├── Repositories/                ← EFChallengeRepository, EFAttemptRepository
│       └── Migrations/                  ← EF Core migrations (pending)
├── tests/
│   ├── DevBrain.Domain.Tests/           ← Domain entity tests (30)
│   ├── DevBrain.Infrastructure.Tests/   ← Repository & DbContext tests (39)
│   └── DevBrain.Api.Tests/              ← Integration endpoint tests (26)
├── specs/                               ← Spec files (SDD methodology)
│   ├── domain/                          ← Entity specs
│   ├── api/                             ← Endpoint specs
│   └── infrastructure/                  ← Repository specs
├── postman/                             ← API collection for testing
└── context.md                           ← Full project status
```

---

## API Endpoints

### Challenges

| Method | Endpoint | Status | Tests |
|--------|----------|--------|-------|
| GET | `/api/v1/challenges` | ✅ | 13 |
| POST | `/api/v1/challenges/{id}/attempt` | ✅ | 26 |

**GET /api/v1/challenges** — List all challenges with pagination & filtering
```bash
curl "http://localhost:5000/api/v1/challenges?category=Sql&difficulty=Easy&page=1&pageSize=10"
```

Response: `200 OK`
```json
{
  "items": [...],
  "totalCount": 27,
  "page": 1,
  "pageSize": 10
}
```

**POST /api/v1/challenges/{id}/attempt** — Submit an attempt
```bash
curl -X POST "http://localhost:5000/api/v1/challenges/{id}/attempt" \
  -H "X-User-Id: test_user_123" \
  -H "Content-Type: application/json" \
  -d '{"userAnswer":"7","elapsedSeconds":45}'
```

Response: `201 Created`
```json
{
  "attemptId": "uuid",
  "challengeId": "uuid",
  "userId": "test_user_123",
  "userAnswer": "7",
  "isCorrect": true,
  "correctAnswer": "7",
  "elapsedSeconds": 45,
  "challengeTitle": "Memory: Loop Counting",
  "occurredAt": "2026-04-09T12:00:00Z"
}
```

---

## Categorías de Desafíos

- **SQL / Bases de datos** — queries, optimización, detección de errores
- **Lógica de código** — ¿qué imprime este código?, encontrar bugs, completar métodos
- **Arquitectura / Diseño** — elegir la solución correcta, detectar anti-patterns
- **Docker / DevOps** — Dockerfiles con errores, docker-compose, secuencias de comandos
- **Memoria de trabajo** — tracing de variables, aplicar reglas de negocio

---

## Gamificación (Próximas Features)

- Streak diario (racha de días consecutivos)
- Rating ELO por categoría
- Tiempo límite por problema (presión real de trabajo)
- Explicación post-respuesta
- Modo "sprint": 5 problemas en 3 minutos
- Logros / badges

---

## Tech Stack

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 10 (C#) |
| Database | PostgreSQL (+ Redis para streaks) |
| Testing | xUnit 2.9.3 |
| ORM | EF Core 10 |
| Frontend | Next.js + Tailwind (próximo) |
| Deploy | **Azure App Service** (backend) + GitHub Pages (frontend) |
| Auth | Supabase Auth (JWT) |

---

## Development Methodology

**SDD + TDD**: Spec-Driven Development + Test-Driven Development

1. **Spec** — Define contract in `.spec.md` file
2. **Tests** — Write xUnit tests (red phase)
3. **Implement** — Code to make tests pass (green phase)
4. **Update Context** — Document status in `context.md`
5. **Commit & Push** — Record in git with descriptive messages

All feature specs are in `specs/` directory. See `context.md` for current roadmap.

---

## Completed Features ✅

- [x] Domain entities (Challenge, Attempt, User, Badge)
- [x] EF Core repositories + DbContext with seed data
- [x] GET /challenges (with filtering & pagination)
- [x] POST /challenges/{id}/attempt (with ELO & badge awarding)
- [x] POST /auth/register (with PBKDF2 hashing)
- [x] POST /auth/login (JWT generation)
- [x] GET /users/me/stats (accuracy, streak, ELO)
- [x] PostgreSQL integration (Neon in production)
- [x] Redis integration (streaks, caching)
- [x] Azure App Service deployment (production)
- [x] Serilog + Application Insights logging
- [x] E2E Integration Tests with TestContainers

## Next Steps (Phase 3.3+)

- [ ] **Endpoint logging integration** — Add ILogger<T> injection to all endpoints
- [ ] Frontend (Next.js) with challenge UI
- [ ] Dynamic challenge generation via Claude API
- [ ] Advanced leaderboards & filtering
- [ ] User analytics dashboard
- [ ] Mobile app (Flutter or React Native)
- [ ] Redis integration for performance

See [`context.md`](./context.md) for details and current progress.
