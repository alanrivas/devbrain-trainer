# DevBrain Trainer — Stack Tecnológico Completo

> Guía maestra de todas las herramientas, configuraciones y dependencias del proyecto.

---

## 1. Backend: ASP.NET Core 10

### ¿Qué es?
Framework web moderno de Microsoft para APIs REST. Versión 10 es la más reciente (2024).

### ¿Por qué?
- **Performance**: Uno de los franquicios web más rápidos
- **Typing**: C# fuertemente tipado vs JavaScript
- **Built-in DI**: Inyección de dependencias nativa
- **Entity Framework**: ORM poderoso integrado
- **Cloud-first**: Integración perfecta con Azure

### Configuración
- **Runtime**: .NET 10.0
- **Lenguaje**: C# 13 (nullable enabled)
- **Csproj**: `src/DevBrain.Api/DevBrain.Api.csproj`
- **Entry point**: `src/DevBrain.Api/Program.cs`

### Punto de entrada
```bash
cd c:\dev\devbrain-trainer
dotnet run --project src/DevBrain.Api/
# Escucha en http://localhost:5000
```

### Documentación oficial
- https://learn.microsoft.com/en-us/aspnet/core/
- https://github.com/dotnet/aspnetcore

---

## 2. Base de Datos: PostgreSQL

### ¿Qué es?
Base de datos relacional open-source. Excelente para queries complejas y ACID compliance.

### Configuración por ambiente

| Ambiente | Host | Puerto | Usuario | Base | SSL |
|----------|------|--------|---------|------|-----|
| **Desarrollo local** | localhost | 5433 | postgres | devbrain_dev | No |
| **Testing** | docker (testcontainers) | random | postgres | postgres_test | No |
| **Producción (Azure+Neon)** | `ep-*.us-east-1.neon.tech` | 5432 | user | devbrain_prod | **Sí** |

### Herramienta de configuración: Entity Framework Core

#### Setup inicial (Development)
```bash
# Aplicar migraciones a base local
dotnet ef database update --project src/DevBrain.Infrastructure/

# Ver estado
dotnet ef migrations list --project src/DevBrain.Infrastructure/
```

#### Crear nueva migración
```bash
# Después de cambiar modelo (Domain entities)
dotnet ef migrations add NombreDelCambio --project src/DevBrain.Infrastructure/ --startup-project src/DevBrain.Api/

# Revisar
dotnet ef migrations script --from [anterior] --to [nuevo] --project src/DevBrain.Infrastructure/

# Aplicar
dotnet ef database update --project src/DevBrain.Infrastructure/
```

### Connection String (variables de entorno)

#### Local
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=devbrain_dev;Username=postgres;Password=postgres;SSL Mode=Disable"
```

#### Azure (almacenado en Key Vault)
```
Host=ep-xxx.us-east-1.neon.tech;Database=devbrain_prod;Username=user;Password=***;SSL Mode=Require;Trust Server Certificate=true
```

### Migraciones de proyecto
- `InitialCreate` — Usuarios, Challenges, Attempts, Badges, UserBadges (seed: 10 challenges)
- `AddEloRatingToUser` — Agregar columna EloRating con valor default 1000

### Documentación
- https://www.postgresql.org/docs/
- https://learn.microsoft.com/en-us/ef/core/

---

## 3. Cache: Redis

### ¿Qué es?
Store in-memory para datos que se acceden frecuentemente (streaks, sesiones).

### Configuración por ambiente

| Ambiente | Host | Puerto | SSL |
|----------|------|--------|-----|
| **Desarrollo local** | localhost | 6379 | No |
| **Testing** | docker (testcontainers) | random | No |
| **Producción (Azure+Redis Cloud)** | `redis-xxx.c1.us-east-1-2.ec2.cloud.redislabs.com` | 6379 | **Sí** + Auth |

### Servicio wrapper: `IStreakService`
```csharp
// En Infrastructure/Services/RedisStreakService.cs
public interface IStreakService
{
    Task<int> GetStreakAsync(Guid userId);
    Task RecordAttemptAsync(Guid userId, bool isCorrect);
    Task ResetStreakAsync(Guid userId);
}

// Implementación usa StackExchange.Redis internamente
```

### Para Testing
Usamos `MockStreakService` (diccionario en memoria) para aislamiento:
```csharp
// En Integration.Tests/MockStreakService.cs
public class MockStreakService : IStreakService
{
    private readonly Dictionary<Guid, (DateTimeOffset, int)> _streaks = new();
}
```

### Connection String
```csharp
// En Program.cs, se lee de config
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
```

### Documentación
- https://redis.io/docs/
- https://stackexchange.github.io/StackExchange.Redis/

---

## 4. Testing: xUnit + TestContainers

### xUnit
Framework de testing .NET moderno. Similar a Jest/Mocha pero para C#.

#### Estructura test
```csharp
public class MyChallengeTests
{
    [Fact]
    public void GetById_WithValidId_ShouldReturnChallenge()
    {
        // Arrange
        var challenge = Challenge.Create(...);

        // Act
        var result = challenge.IsCorrectAnswer("answer");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("answer1")]
    [InlineData("answer2")]
    public void IsCorrectAnswer_WithVariations_ShouldMatch(string answer)
    {
        // ...
    }
}
```

#### Ejecutar tests
```bash
# Todos
dotnet test

# Suite específica
dotnet test tests/DevBrain.Domain.Tests/

# Test específico
dotnet test --filter "TestName"

# Con cobertura
dotnet test /p:CollectCoverage=true
```

### TestContainers
Framework para levantar contenedores Docker reales para tests de integración.

#### NuGet packages
```bash
dotnet add package Testcontainers
dotnet add package Testcontainers.PostgreSQL
dotnet add package Testcontainers.Redis
```

#### Ciclo de vida (en Integration Tests)
```csharp
public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        // 1. Inicia contenedores
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // 2. Aplica migraciones
        await RunMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        // Detiene limpiamente
        await _postgres.StopAsync();
        await _redis.StopAsync();
    }
}
```

### Test Suites Status
```
Domain.Tests:        69/69 ✅
Infrastructure.Tests: 53/53 ✅
Api.Tests:           83/83 ✅
Integration.Tests:    2/2 ✅
────────────────────────────
TOTAL:             207/207 ✅
```

### Documentación
- https://xunit.net/
- https://testcontainers.com/

---

## 5. Logging: Serilog + Application Insights

### Serilog
Logging estructurado que escribe JSON para parsing automático.

#### NuGet packages
```bash
dotnet add package Serilog
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.ApplicationInsights
dotnet add package Serilog.Enrichers.Environment
```

#### Configuración (Program.cs)
```csharp
var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentUserName()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(
        new TelemetryClient(),
        TelemetryConverter.Traces
    )
    .CreateLogger();

Log.Logger = logger;
builder.Host.UseSerilog(logger);
```

#### Uso en código
```csharp
public class AuthEndpoints
{
    private readonly ILogger<AuthEndpoints> _logger;

    public AuthEndpoints(ILogger<AuthEndpoints> logger)
    {
        _logger = logger;
    }

    public async Task Register(RegisterRequest request)
    {
        _logger.LogInformation("📝 Register: {Email}", request.Email);
        // ...
        _logger.LogInformation("✅ User registered: {UserId}", user.Id);
    }
}
```

#### Niveles
```
Debug    → Solo desarrollo
Info     → Eventos normales (user registered, challenge created)
Warning  → Algo inusual (email duplicate, request slow)
Error    → Fallo (DB timeout, JWT invalid)
Fatal    → Crash crítico (app no puede recuperarse)
```

### Application Insights (Azure)
Dashboard centralizado para logs, metrics, performance.

#### Configuración
```bash
# Resource group: devbrain-rg (Azure)
# Application Insights: devbrain-insights
# Instrumentation Key: [en Azure Key Vault]
```

#### Cómo ver logs
1. **Azure Portal** → App Service → Application Insights → Logs
2. **Queries Kusto**:
   ```kusto
   traces
   | where message contains "Register"
   | order by timestamp desc
   ```

#### Alertas
```
Condición: requests | where duration > 2000
Acción: Email a alan@example.com
```

### Documentación
- https://serilog.net/
- https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview

---

## 6. Deploy: Azure App Service

### ¿Qué es?
Servicio gestionado de Azure para hostear aplicaciones web (PaaS).

### Configuración
- **Tier**: Free (F1) en desarrollo, Standard en producción
- **OS**: Linux
- **Runtime**: .NET 10
- **Region**: East US
- **Resource Group**: devbrain-rg

### Deployment (CI/CD)
```
Git push main
  ↓
GitHub Actions (workflow: .azure-deploy.yml)
  ↓
dotnet publish
  ↓
Azure App Service reinicia con nueva versión
```

### Monitoreo
- **URL**: https://devbrain-trainer.azurewebsites.net/
- **Health check**: GET /health
- **Docs API**: GET /scalar

### Variables de entorno (en Azure)
```
ConnectionStrings__DefaultConnection = Host=ep-xxx...
Redis__ConnectionString = redis-xxx...
Jwt__Secret = [super-secret-32-chars]
```

### Documentación
- https://learn.microsoft.com/en-us/azure/app-service/

---

## 7. ORM: Entity Framework Core

### ¿Qué es?
Object-Relational Mapper que mapea entidades C# a tablas SQL automáticamente.

### Estructura
```csharp
// Domain layer (sin EF)
public sealed record Challenge
{
    public Guid Id { get; init; }
    public string Question { get; init; }
    public string CorrectAnswer { get; init; }
}

// Infrastructure layer (con EF)
public class DevBrainDbContext : DbContext
{
    public DbSet<Challenge> Challenges { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Challenge>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Challenge>()
            .HasData(
                new Challenge { Id = Guid.NewGuid(), Question = "...", ... }
            );
    }
}
```

### Repositorios (patrón)
```csharp
// Domain interface (sin detalles EF)
public interface IChallengeRepository
{
    Task<Challenge?> GetByIdAsync(Guid id);
    Task AddAsync(Challenge challenge);
    Task SaveChangesAsync();
}

// Infrastructure implementation
public class EFChallengeRepository : IChallengeRepository
{
    private readonly DevBrainDbContext _context;

    public async Task<Challenge?> GetByIdAsync(Guid id)
    {
        return await _context.Challenges
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
```

### Migrations workflow
```
1. Cambiar modelo (Domain)
2. dotnet ef migrations add [nombre]
3. Revisar archivo [timestamp]_[nombre].cs
4. dotnet ef database update
```

### Documentación
- https://learn.microsoft.com/en-us/ef/core/

---

## 8. API: OpenAPI + Scalar

### OpenAPI (Swagger)
Especificación estándar para documentar APIs REST.

#### Endpoint
```
GET /openapi/v1.json
```

#### Scalar (UI visual)
```
GET /scalar
```

Herramienta web que visualiza OpenAPI en interfaz interactiva.

### Documentación
- https://www.openapis.org/
- https://scalar.dev/

---

## 9. Frontend: Next.js + Tailwind (Próximo)

### Next.js 15
Framework React con SSR, routing file-based, API routes integradas.

### Tailwind CSS
Utility-first CSS framework (clases vs escribir CSS manual).

### Deployment
GitHub Pages (estática) o Vercel (recomendado para Next.js dinámico).

### Status
🚧 Aún no implementado (Fase 4 del roadmap)

---

## 10. Auth: JWT (propio)

### ¿Qué es?
JSON Web Tokens para autenticación stateless. Actualmente implementado en-house en `JwtTokenService`.

### Estructura
```csharp
// En Infrastructure/Services/JwtTokenService.cs
var token = jwtService.GenerateToken(user.Id, user.Email);
// Token válido por 24 horas
```

### Por qué no Supabase Auth (aún)
- Complejidad extra para MVP
- JWT probpio = control total
- Auth local funciona para testing

### Planes futuros
Migrar a Supabase Auth (mejor escalabilidad).

---

## 11. API Testing: Postman

### Collection
- **Archivo**: `postman/devbrain-trainer.postman_collection.json`
- **Environments**:
  - `devbrain-trainer.localhost.postman_environment.json` (dev)
  - `devbrain-trainer.production.postman_environment.json` (prod)

### Import a Postman
```
Postman App → Import → Select JSON file
```

### Requests
- Registro, login, obtener challenges, enviar attempt, etc.

---

## 12. Metodología: SDD + TDD

| Fase | Artefacto | Responsabilidad |
|------|-----------|-----------------|
| **Spec** | `.spec.md` | QUÉ: contrato de la feature (no implementación) |
| **Test** | `*Tests.cs` (xUnit) | Validar spec con Arrange-Act-Assert |
| **Impl** | Código en `src/` | Hacer que los tests pasen (VERDE) |
| **Context** | `context.md` update | Registrar progreso y próximo paso |

### Workflow
```
1. write-spec skill → crear .spec.md
2. spec-implement skill → genera tests + código + commit + push
3. update-context skill → actualizar context.md
```

### Skills disponibles
- `write-spec` — Crear especificación
- `spec-implement` — Tests + Implementación + Push
- `update-context` — Actualizar estado del proyecto

---

## 13. Git & GitHub

### Repo
```
https://github.com/alanrivas/devbrain-trainer
```

### Branching
- `main` — Producción (todo merged debe estar testeado)
- Feature branches por feature (no usado aún, todo en main for now)

### CI/CD
- GitHub Actions para deploy a Azure
- No está configurado PR checks, pero recomendado para futuro

---

## 📊 Tabla de referencia rápida

| Herramienta | Comando | Docs | Status |
|-------------|---------|------|--------|
| .NET 10 | `dotnet run` | [link](https://learn.microsoft.com/en-us/dotnet/) | ✅ Implementado |
| PostgreSQL | `psql` | [link](https://www.postgresql.org/docs/) | ✅ Neon en prod |
| Redis | `redis-cli` | [link](https://redis.io/docs/) | ✅ Redis Cloud en prod |
| xUnit | `dotnet test` | [link](https://xunit.net/) | ✅ 207/207 tests |
| TestContainers | [code] | [link](https://testcontainers.com/) | ✅ E2E tests |
| Serilog | [config] | [link](https://serilog.net/) | ⏳ Pendiente implementar |
| App Insights | Azure Portal | [link](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) | ✅ Ready en Azure |
| EF Core | `dotnet ef` | [link](https://learn.microsoft.com/en-us/ef/core/) | ✅ Implementado |
| Azure App Service | Portal | [link](https://learn.microsoft.com/en-us/azure/app-service/) | ✅ Deployado |
| Next.js | [próximo] | [link](https://nextjs.org/docs) | 🚧 Fase 4 |
| JWT | [código] | [link](https://jwt.io/) | ✅ Propio |

---

## 🔗 Guías rápidas

### Problema: Tests fallan
```bash
# Limpia build cache
dotnet clean
dotnet build

# Re-ejecuta tests
dotnet test --verbosity normal
```

### Problema: DB no conecta
```bash
# Verifica connection string
echo $env:ConnectionStrings__DefaultConnection

# Testa conexión
psql -h localhost -U postgres -d devbrain_dev
```

### Problema: Redis no funciona
```bash
# Verifica servicio
redis-cli ping
# Debe responder: PONG
```

### Problema: App crashes en Azure
```bash
# Ver logs en Azure Portal
Portal → App Service → Log Stream
# O búsqueda en Application Insights
```

---

## ✅ Checklist de setup completo

- [ ] .NET 10 instalado (`dotnet --version`)
- [ ] PostgreSQL local corriendo (puerto 5433)
- [ ] Redis local corriendo (puerto 6379)  
- [ ] Connection strings configuradas
- [ ] `dotnet build` sin errores
- [ ] `dotnet test` → 207/207 ✅
- [ ] `dotnet run` → API en http://localhost:5000
- [ ] `GET /scalar` → OpenAPI funciona

---

**Última actualización**: 2026-04-10  
**Versión**: 1.0  
**Mantenedor**: Alan Rivas
