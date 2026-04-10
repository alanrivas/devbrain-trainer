# Architecture — DevBrain Trainer

Descripción de la arquitectura, layers y decisiones de diseño.

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Frontend Layer                        │
│  Next.js + Tailwind (React) — Challenge UI, Leaderboards    │
│  Hosted on: GitHub Pages / Vercel                            │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTP REST API
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                      API Layer (ASP.NET Core 10)             │
│  - Endpoints: Challenge, Attempt, Auth, User                │
│  - DTOs: Request/Response contracts                          │
│  - Middleware: JWT auth, CORS, error handling               │
│  - Logging: Serilog → Console, File, Application Insights   │
│  Hosted on: Azure App Service (Standard tier)                │
└────────────────────────┬────────────────────────────────────┘
                         │
        ┌────────────────┼────────────────┐
        ↓                ↓                ↓
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  PostgreSQL  │  │    Redis     │  │   App        │
│   (Neon)     │  │ (Redis Cloud)│  │ Insights     │
│              │  │              │  │ (Telemetry)  │
│  - Users     │  │  - Streaks   │  │              │
│  - Challenges│  │  - Cache     │  │  - Logs      │
│  - Attempts  │  │              │  │  - Metrics   │
│  - Badges    │  │              │  │              │
└──────────────┘  └──────────────┘  └──────────────┘
```

---

## Layered Architecture (DDD)

### Layer 1: Presentation (API Layer)

**Location**: `src/DevBrain.Api/`

**Responsibilities**:
- HTTP request/response handling
- DTO mapping
- Route definitions
- Authentication middleware
- Error formatting
- Logging initialization

**Key Files**:
- `Program.cs` — Application startup, DI configuration
- `Endpoints/*.cs` — Route handlers (minimal APIs)
- `DTOs/` — Request and response contracts
- `Mapping/` — Entity ↔ DTO conversion

**Example**:
```csharp
// src/DevBrain.Api/Endpoints/ChallengeEndpoints.cs
app.MapGet("/api/v1/challenges", GetChallenges)
    .WithName("GetChallenges")
    .WithOpenApi();

async Task<Result> GetChallenges(
    IChallengeRepository repo,
    ILogger<ChallengeEndpoints> logger,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    logger.LogInformation("Fetching challenges page {Page}", page);
    var challenges = await repo.GetAllAsync(page, pageSize);
    return Results.Ok(challenges.Select(c => new ChallengeDto(...)));
}
```

### Layer 2: Domain Layer (Business Logic)

**Location**: `src/DevBrain.Domain/`

**Responsibilities**:
- Domain entities (Challenge, User, Attempt, Badge)
- Business rules and invariants
- Enum models (Category, Difficulty, BadgeType)
- Exception types (DomainException)
- Repository interfaces (contracts, not implementation)

**Key Files**:
- `Entities/` — Challenge, User, Attempt, Badge records
- `Interfaces/` — IChallengeRepository, IAttemptRepository, IUserRepository
- `Enums/` — ChallengeCategory, Difficulty
- `Exceptions/` — DomainException
- `Services/` — EloRatingService, BadgeAwardService

**Example**:
```csharp
// src/DevBrain.Domain/Entities/Challenge.cs
public record Challenge(
    Guid Id,
    string Title,
    string Description,
    string CorrectAnswer,
    ChallengeCategory Category,
    Difficulty Difficulty,
    int TimeLimitSecs)
{
    public bool IsCorrectAnswer(string userAnswer) 
        => userAnswer.Trim().Equals(CorrectAnswer, StringComparison.OrdinalIgnoreCase);
}
```

### Layer 3: Infrastructure Layer (Persistence & External Services)

**Location**: `src/DevBrain.Infrastructure/`

**Responsibilities**:
- Database context (EF Core)
- Repository implementations
- Database migrations
- External service integrations (Redis, email, etc.)
- Repositories *implement* domain interfaces

**Key Files**:
- `Persistence/DevBrainDbContext.cs` — EF Core configuration
- `Repositories/` — EFChallengeRepository, EFAttemptRepository, etc.
- `Migrations/` — EF Core migration files

**Example**:
```csharp
// src/DevBrain.Infrastructure/Repositories/EFChallengeRepository.cs
public sealed class EFChallengeRepository : IChallengeRepository
{
    private readonly DevBrainDbContext _context;
    
    public async Task<Challenge> GetByIdAsync(Guid id)
    {
        return await _context.Challenges.FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task AddAsync(Challenge challenge)
    {
        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();
    }
}
```

---

## Dependency Injection Pattern

**Registered in `Program.cs`**:

```csharp
// Domain services
builder.Services.AddScoped<EloRatingService>();
builder.Services.AddScoped<BadgeAwardService>();
builder.Services.AddScoped<StreakService>();

// Infrastructure repositories
builder.Services.AddScoped<IChallengeRepository, EFChallengeRepository>();
builder.Services.AddScoped<IAttemptRepository, EFAttemptRepository>();
builder.Services.AddScoped<IUserRepository, EFUserRepository>();

// Persistence
builder.Services.AddDbContext<DevBrainDbContext>(options =>
    options.UseNpgsql(connString));

// Caching
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = redisConnection);

// Logging
builder.Host.UseSerilog(_logger);

// Identity & Auth
builder.Services.AddJwtBearer(...);
```

---

## Data Flow

### Flow 1: Register User

```
Request (HTTP POST)
  ↓
AuthEndpoints.Register()
  ├─ Validate: Email, password, displayName
  ├─ Call: IUserRepository.GetByEmailAsync(email)
  │   └─ Check if user already exists
  ├─ Create: User entity with PBKDF2 hash
  ├─ Call: IUserRepository.AddAsync(user)
  │   └─ Persist to PostgreSQL
  └─ Return: UserDto + JWT token
  ↓
Response (HTTP 201 Created)
```

### Flow 2: Submit Attempt

```
Request (HTTP POST /challenges/{id}/attempt)
  ↓
ChallengeEndpoints.PostAttempt()
  ├─ Extract: X-User-Id header (user authentication)
  ├─ Fetch: Challenge by ID from PostgreSQL
  ├─ Create: Attempt entity (UserAnswer, IsCorrect, ElapsedSecs)
  ├─ Evaluate: Challenge.IsCorrectAnswer(userAnswer)
  ├─ Update: User ELO rating via EloRatingService
  ├─ Award: Badge (if conditions met) via BadgeAwardService
  ├─ Update: Streak in Redis via StreakService
  ├─ Log: Serilog context enriched with UserId, ChallengeId
  └─ Persist: Attempt to PostgreSQL
  ↓
Response (HTTP 201 Created + AttemptDto)
```

### Flow 3: Get User Stats

```
Request (HTTP GET /users/me/stats)
  ↓
UserEndpoints.GetStats()
  ├─ Extract: X-User-Id header
  ├─ Fetch: All attempts by user from PostgreSQL
  ├─ Calculate: totalAttempts, correctAttempts, accuracyRate (0-100%)
  ├─ Fetch: Streak from Redis (cached)
  ├─ Fetch: Badges from PostgreSQL
  └─ Return: UserStatsDto with ELO, streak, accuracy
  ↓
Response (HTTP 200 OK + UserStatsDto)
```

---

## Database Schema

```sql
-- Users
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(512) NOT NULL,
    display_name VARCHAR(100),
    elo_rating INT DEFAULT 1000,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Challenges
CREATE TABLE challenges (
    id UUID PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    correct_answer VARCHAR(1000),
    category VARCHAR(50),
    difficulty VARCHAR(50),
    time_limit_secs INT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Attempts
CREATE TABLE attempts (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    challenge_id UUID REFERENCES challenges(id),
    user_answer TEXT,
    is_correct BOOLEAN,
    elapsed_secs INT,
    occurred_at TIMESTAMP DEFAULT NOW()
);

-- Badges
CREATE TABLE user_badges (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    badge_type VARCHAR(50),
    earned_at TIMESTAMP DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX idx_attempts_user ON attempts(user_id);
CREATE INDEX idx_attempts_challenge ON attempts(challenge_id);
CREATE INDEX idx_badges_user ON user_badges(user_id);
```

---

## Caching Strategy

### Redis Usage

| Key Pattern | Value | TTL | Use Case |
|---|---|---|---|
| `streak:{userId}` | `{"count": 5, "lastAttemptAt": "2026-04-10"}` | 24h | Streak tracking |
| `user:elo:{userId}` | `1050` | 1h | ELO caching (not critical) |
| `challenges:list` | `[Challenge...]` | 6h | Challenge list cache |

**Invalidation**:
- Streak: Updated on every attempt
- ELO: Invalidated when user rank changes
- Challenges: Invalidated when admin creates new challenge

---

## Error Handling

### Exception Hierarchy

```
Exception
├─ DomainException          ← Business rule violations
│  ├─ InvalidEmail
│  ├─ InvalidPassword
│  └─ DuplicateUser
├─ RepositoryException      ← Data access errors
└─ ServiceException         ← External service errors
```

### HTTP Error Responses

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Email is required and must be valid"
}
```

**Status Codes**:
- `200 OK` — Success
- `201 Created` — Resource created
- `400 Bad Request` — Validation failed
- `401 Unauthorized` — Token missing/invalid
- `404 Not Found` — Resource not found
- `409 Conflict` — Duplicate email
- `500 Internal Server Error` — Unhandled exception

---

## Logging Architecture

### Serilog Configuration

```csharp
// Program.cs
var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentUserName()
    .Enrich.WithProperty("Environment", env.EnvironmentName)
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File("logs/devbrain-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(telemetryClient, TelemetryConverter.Events)
    .CreateLogger();
```

### Log Enrichment

Every log includes:
- `Timestamp` — When the event occurred
- `Level` — Debug, Information, Warning, Error, Fatal
- `SourceContext` — Logger name (namespace)
- `EnvironmentUserName` — Current user
- `Environment` — Dev, Testing, Production
- `UserId` — From LogContext (for correlation)
- `RequestId` — For tracing requests

---

## Testing Architecture

### Test Projects

```
tests/
├── DevBrain.Domain.Tests/         ← Entity & service logic
│   └── 69 tests
├── DevBrain.Infrastructure.Tests/ ← Repository & DbContext
│   └── 58 tests
├── DevBrain.Api.Tests/            ← Endpoint integration
│   └── 83 tests
└── DevBrain.Integration.Tests/    ← E2E with TestContainers
    └── 2 tests (PostgreSQL + Redis)
```

### Test Fixtures

**In-Memory Database**:
```csharp
var options = new DbContextOptionsBuilder<DevBrainDbContext>()
    .UseInMemoryDatabase("test-db")
    .Options;
var context = new DevBrainDbContext(options);
```

**TestContainers**:
```csharp
var postgres = new PostgresBuilder()
    .WithImage("postgres:17-alpine")
    .Build();
var redis = new RedisBuilder()
    .WithImage("redis:7-alpine")
    .Build();
```

---

## Deployment Architecture

### Local Development

```
Developer Machine
├─ Visual Studio Code / Rider
├─ .NET CLI (dotnet run)
├─ PostgreSQL (Docker: postgres:17)
└─ Redis (Docker: redis:7)
```

**Command**:
```bash
docker-compose up -d  # Start PostgreSQL & Redis
dotnet run --project src/DevBrain.Api/
```

### Production

```
Azure App Service (F1 → Standard tier)
├─ Backend: ASP.NET Core 10 (native .NET publish, not Docker)
├─ Configuration: Environment variables
└─ Monitoring: Application Insights

PostgreSQL: Neon.tech (serverless, scales to 0)
Redis: Redis Cloud (managed cache)
Frontend: GitHub Pages / Vercel (Next.js)
```

---

## Performance Optimizations

| Issue | Solution | Status |
|---|---|---|
| N+1 queries | EF Core `.Include()` eager loading | ✅ Implemented |
| Streak lookups | Redis caching (24h TTL) | ✅ Implemented |
| Challenge filters | Database indexes on category, difficulty | ✅ Indexes created |
| Auth on every request | JWT token in header (no DB lookup) | ✅ Implemented |
| Slow migrations | Migration scripts pre-generated | ✅ Pending |

---

## Future Architecture Improvements

- [ ] Event sourcing for audit trail
- [ ] Message queue (RabbitMQ) for async tasks
- [ ] GraphQL API alongside REST
- [ ] Microservices: separate Auth, Challenge, Leaderboard services
- [ ] CDN for static challenge assets
- [ ] Real-time leaderboards via WebSockets/SignalR
